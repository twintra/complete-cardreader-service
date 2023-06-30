//
// csc wsserver.cs
// wsserver.exe

using PCSC.Exceptions;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TestServer;
using ThaiNationalIDCard;

class Server
{
    public static void Main2()
    {
        string ip = "127.0.0.1";
        int port = 7023;
        var server = new TcpListener(IPAddress.Parse(ip), port);

        server.Start();
        Console.WriteLine("Server has started on {0}:{1}, Waiting for a connection…", ip, port);
        List<TcpClient> listconnectedClients = new List<TcpClient>();

        

        while (true)
        {

            while (listconnectedClients.Count!= 0)
            {
                listconnectedClients[listconnectedClients.Count-1].Close();
            }
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("A client connected.");
            listconnectedClients.Add(client);

            NetworkStream stream = client.GetStream();
            var idcard = new ThaiIDCard();

            var exp_time = DateTime.Now.AddMinutes(30);

            // enter to an infinite cycle to be able to handle every change in stream
            
            while (true)
            {


                bool disconnected = false;
                while (!stream.DataAvailable)
                {
                    var now = DateTime.Now;
                    int is_exp = exp_time.CompareTo(now);

                    if (is_exp <= 0)
                    {
                        // Console.WriteLine("time out");
                        string response = createJsonString("reconnect");
                        SendMessageToClient(stream, response);
                        onDisconnect(idcard, stream, client, listconnectedClients);
                        disconnected = true;
                        break;
                    }
                } ;

                if (disconnected) break;
                while (client.Available < 3); // match against "get"

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, client.Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);

                    // after done handshake, do check reader.
                    doCardReader(idcard, stream);
                }
                else
                {
                    Console.WriteLine("Else");
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                    int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                        offset = 2;
                    ulong msglen = (ulong)bytes[1] & 0b01111111;

                    if (msglen == 126)
                    {
                        Console.WriteLine("msglen == 126");

                        // bytes are reversed because websocket will print them in Big-Endian, whereas
                        // BitConverter will want them arranged in little-endian on windows
                        msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        offset = 4;
                    }
                    else if (msglen == 127)
                    {
                        Console.WriteLine("msglen == 127");

                        // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                        // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                        // websocket frame available through client.Available).
                        msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                        offset = 10;
                    }

                    if (msglen == 0)
                    {
                        Console.WriteLine("msglen == 0");
                    }
                    else if (mask)
                    {
                        Console.WriteLine("else if mask");
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for (ulong i = 0; i < msglen; ++i)
                        {
                            decoded[i] = (byte)(bytes[((ulong)offset) + i] ^ masks[i % 4]);
                        }

                        // check if client disconnected
                        if (decoded.Length == 2 && decoded[0] == 3 && decoded[1] == 233) 
                        {
                            Console.WriteLine("Client disconnected");
                            onDisconnect(idcard, stream, client, listconnectedClients);
                            break;
                        }

                        string text = Encoding.UTF8.GetString(decoded);

                        if(text == "res")
                        {
                            Console.WriteLine("client want response");
                            string res = createJsonString("Service actives!", keyname: "message");
                            SendMessageToClient(stream, res );
                        }
                        if(text == "reader")
                        {
                            Console.WriteLine("Client want to connect to reader");
                            doCardReader(idcard, stream);
                        }

                        if (text == "disconnect")
                        {
                            Console.WriteLine("Client send disconnect");
                            onDisconnect(idcard, stream, client, listconnectedClients);
                            break;
                        }
                        if (text == "showClientList")
                        {
                            Console.WriteLine(listconnectedClients.Count);
                        }
                        

                        Console.WriteLine("{0}", text);

                    }
                    else
                        Console.WriteLine("mask bit not set");

                    Console.WriteLine();
                }

                
                

            }

        }
        
    }

    public static void onDisconnect(ThaiIDCard idcard, NetworkStream stream, TcpClient client, List<TcpClient> listconnectedClients) 
    {
        stopCardReader(idcard, stream);
        listconnectedClients.Remove(client);
        stream.Close();
        client.Close();

    } 


    public static string createJsonString(string message, string keyname = "message")
    {

        Dictionary<string, string> json = new Dictionary<string, string>();
        json.Add(keyname, message);
        string jsonString = DictionaryToJson(json);
        return jsonString;
    }
    public static void doCardReader(ThaiIDCard idcard, NetworkStream stream)
    {
        try
        {
            string[] readers = idcard.GetReaders();
            string res = createJsonString("reader connected");
            SendMessageToClient(stream, res);
            Console.WriteLine("readers connected : {0}", readers);
            if (readers.Length != 0)
            {
                for (int i = 0; i < readers.Length; i++)
                {
                    idcard.MonitorStart(readers[i]);
                }
            }
            idcard.eventCardInsertedWithPhoto += (e) =>
            {
                Console.WriteLine("Card inserted");
                Personal personal = e;
                Dictionary<String, String> data = new Dictionary<String, String>();
                if (personal != null)
                {

                    String string_photo = Convert.ToBase64String(personal.PhotoRaw);
                    data.Add("message", "Card inserted");
                    data.Add("cid", personal.Citizenid);
                    data.Add("birthday", personal.Birthday?.ToString("dd/MM/yyyy"));
                    data.Add("sex", personal.Sex);
                    data.Add("th_prefix", personal.Th_Prefix);
                    data.Add("th_firstname", personal.Th_Firstname);
                    data.Add("th_lastname", personal.Th_Lastname);
                    data.Add("en_prefix", personal.En_Prefix);
                    data.Add("en_firstname", personal.En_Firstname);
                    data.Add("en_lastname", personal.En_Lastname);
                    data.Add("issue", personal.Issue.ToString("dd/MM/yyyy"));
                    data.Add("expire", personal.Expire.ToString("dd/MM/yyyy"));
                    data.Add("address", personal.Address);
                    data.Add("addr_house_no", personal.addrHouseNo);
                    data.Add("addr_village_no", personal.addrVillageNo);
                    data.Add("addr_lane", personal.addrLane);
                    data.Add("addr_road", personal.addrRoad);
                    data.Add("addr_tambol", personal.addrTambol);
                    data.Add("addr_amphur", personal.addrAmphur);
                    data.Add("addr_province", personal.addrProvince);
                    data.Add("photo", string_photo);
                    string jsonString = DictionaryToJson(data);

                    SendMessageToClient(stream,jsonString);

                }
            };

            idcard.eventCardRemoved += () =>
            {
                Console.WriteLine("Card removed");
                string res = createJsonString("card removed");
                SendMessageToClient(stream, res);
            };
            
        }
        catch (PCSCException e)
        {
            string error = createJsonString(e.Message + "; Please connect card reader and try again.", keyname: "error");
            SendMessageToClient(stream, error);
        }

       

    }

    public static void stopCardReader(ThaiIDCard idcard, NetworkStream stream)
    {
        try
        {
            string[] readers = idcard.GetReaders();
            Console.WriteLine("readers connected : {0}", readers);
            if (readers.Length != 0)
            {
                for (int i = 0; i < readers.Length; i++)
                {
                    idcard.MonitorStop(readers[i]);
                }
            }
            Console.WriteLine("Card reader stopped");

        }
        catch (PCSCException e)
        {
            Console.WriteLine("Error while stopping card reader : {0}", e.Message);
        }
    }

    public static void SendMessageToClient(NetworkStream stream, string msg)
    {

        Console.WriteLine("sending message : " + msg);
        Queue<string> que = new Queue<string>(msg.SplitInGroups(125));
        int len = que.Count;

        while (que.Count > 0)
        {
            var header = GetHeader(
                que.Count > 1 ? false : true,
                que.Count == len ? false : true
            );

            byte[] list = Encoding.UTF8.GetBytes(que.Dequeue());
            header = (header << 7) + list.Length;
            try
            {
                stream.Write(IntToByteArray((ushort)header), 0, 2);
                stream.Write(list, 0, list.Length);
            }
            catch (Exception e) 
            {
                Console.WriteLine("Sending message to client error : "+e.Message);
            }
           
        }
    }


    protected static int GetHeader(bool finalFrame, bool contFrame)
    {
        int header = finalFrame ? 1 : 0;//fin: 0 = more frames, 1 = final frame
        header = (header << 1) + 0;//rsv1
        header = (header << 1) + 0;//rsv2
        header = (header << 1) + 0;//rsv3
        header = (header << 4) + (contFrame ? 0 : 1);//opcode : 0 = continuation frame, 1 = text
        header = (header << 1) + 0;//mask: server -> client = no mask

        return header;
    }
    protected static byte[] IntToByteArray(ushort value)
    {
        var ary = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ary);
        }

        return ary;
    }

    public static string DictionaryToJson(Dictionary<string, string> dict)
    {
        var entries = dict.Select(d =>
            string.Format("\"{0}\": \"{1}\"", d.Key, string.Join(",", GetUnicodeValue(d.Value) )));
        return "{" + string.Join(",", entries) + "}";
    }

    public static string GetUnicodeValue(string value)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(value) != value.Length)
        {
            string result = "";
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < value.Length; i++)
            {
                result += String.Format("\\u{0:x4}", (int) value[i]);

            }
            return result;
        }
        else return value;
    }
}

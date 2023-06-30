using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThaiNationalIDCard;
using PCSC.Exceptions;

namespace TestServer
{
    
    internal class SocketServerController
    {
        private ThaiIDCard card;
        private TcpClient? connectedClient = null;
        private NetworkStream? connectedClientStream = null;

        private bool isMonitoringReader = false;

        // Constructor
        private string ip;
        private int port;
        public SocketServerController(String ip, int port)
        {
            card = new ThaiIDCard();
            this.ip = ip;
            this.port = port;
        }
        public void Start()
        {
            try
            {
                Console.WriteLine("Initiating server");
                TcpListener listener = new TcpListener(IPAddress.Parse(ip), port);

                listener.Start();
                Console.WriteLine("Server has started on {0}:{1}", ip, port);

                while (true)
                {
                    try
                    {
                        Console.WriteLine("Waiting for a new connection...");
                        TcpClient client = listener.AcceptTcpClient();
                        Console.WriteLine("New client connected.");
                        if(connectedClient != null)
                        {
                            CloseConnection();
                        }
                        connectedClient = client;
                        Thread thread = new Thread(()=> HandleClient());
                        thread.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        // Client managing function
        private void CloseConnection()
        {
            Console.WriteLine("==> Closing connection from connected client");
            StopWatchingCardReader();
            if (connectedClient != null) { 
                connectedClient.Dispose();
                connectedClient.Close(); 
            }
            connectedClient = null;
            if (connectedClientStream != null)
            {
                connectedClientStream.Dispose();
                connectedClientStream.Close();
            }
            connectedClient = null;
            Console.WriteLine("==> Closed connection from connected client");

        }

        public void HandleClient()
        {
            if (connectedClient == null) return;
            Console.WriteLine("Thread Started");
            NetworkStream stream = connectedClient.GetStream();
            connectedClientStream = stream;


            while (true)
            {
                if (connectedClient == null || connectedClientStream == null) return;
                if (!connectedClient.Connected)
                {
                    Console.WriteLine("==> Client's not connected");
                    CloseConnection();
                    return;
                }


                byte[] bytes = new byte[connectedClient.Available];
                if (!connectedClientStream.CanRead) return;
                try
                {
                    connectedClientStream.Read(bytes, 0, connectedClient.Available);
                    string s = Encoding.UTF8.GetString(bytes);
                    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                    {
                        OnHandShakeRequest(s);
                    }
                    else
                    {
                        //Console.WriteLine("==> Req POST");
                        if (bytes.Length == 0) continue;

                        bool fin = (bytes[0] & 0b10000000) != 0;
                        bool mask = (bytes[1] & 0b10000000) != 0;
                        int opcode = bytes[0] & 0b00001111;
                        int offset = 2;
                        ulong msglen = (ulong)bytes[1] & 0b01111111;

                        if (msglen == 126)
                        {
                            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                            offset = 4;
                        }
                        else if (msglen == 127)
                        {
                            msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                            offset = 10;
                        }
                        if (msglen == 0)
                        {
                            Console.WriteLine("msglen == 0");
                        }
                        else if (mask)
                        {
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
                                Console.WriteLine("==> Client disconnected");
                                CloseConnection();
                                return;
                            }

                            string text = Encoding.UTF8.GetString(decoded);

                            HandleClientMessage(text);

                        }
                        else
                        {
                            Console.WriteLine("mask bit not set");
                        }
                        Console.WriteLine();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("==> Error reading stream: {0}", ex.Message);
                    CloseConnection();
                    return;
                }
            }
        }

        private void OnHandShakeRequest(String s)
        {
            if (connectedClientStream == null) return;
            Console.WriteLine("==> Req GET");
            Guid clientid = Guid.NewGuid();
            Console.WriteLine("==> Handshaking from clientid: {0}", clientid);

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

            connectedClientStream.Write(response, 0, response.Length);

            Console.WriteLine("==> swkaSha1Base64: {0}", swkaSha1Base64);

            SendMessage("Handshake successful, connection established");
            StartWatchingCardReader();
        }
        
        private void HandleClientMessage(String message)
        {
            // request data command

            if (message == MessageCommands.STATUS)
            {
                Console.WriteLine("==> Client request response");
            }
            else if (message == MessageCommands.CARD_DATA)
            {
                Console.WriteLine("Client request data");
                if (!isReaderAvailable())
                {
                    SendError(Messages.CARD_READER_NOT_CONNECTED);
                    StopWatchingCardReader();
                    return;
                }
                Dictionary<String, dynamic>? data = ReadPersonalData();
                if (data == null)
                {
                    SendError(Messages.CARD_REMOVED);
                }
                else
                {
                    SendData(data);
                }

            }
            else if (message == MessageCommands.DEVICES)
            {
                Console.WriteLine("==> Client request devices.");
                SendDevice();
            }
            else if (message == MessageCommands.IS_MONITORING)
            {
                if (!isReaderAvailable())
                {
                    SendError(Messages.CARD_READER_NOT_CONNECTED);
                    StopWatchingCardReader();
                }
                SendMessage(isMonitoringReader.ToString());
            }
            // functional command
            else if (message == MessageCommands.START_MONITORING)
            {
                Console.WriteLine("Client requests to start monitoring reader.");
                StartWatchingCardReader();
            }
            else if (message == MessageCommands.STOP_MONITORING)
            {
                Console.WriteLine("Client requests to stop monitoring reader.");
                StopWatchingCardReader();
            }
            else if(message == MessageCommands.DISCONNECT)
            {
                Console.WriteLine("==> Client requests disconnect.");
                CloseConnection();
            }
            else
            {
                Console.WriteLine("Not supported command `{0}`.", message);
                SendError("Not supported request ==> `" + message + "` | Try one of these commands: " + String.Join(", ",MessageCommands.commandsList()));
            }
            
            
            
            
        }
        // ----------

        // Card reader function

        private bool isReaderAvailable()
        {
            try
            {
                string[] readers = card.GetReaders();
                return true;

            } catch (Exception e)
            {
                Console.WriteLine("isReaderAvailable ==> {0}",e.Message);
                return false;
            }

        }

        private void StartWatchingCardReader()
        {
            if(isMonitoringReader) return;
            if (connectedClient == null) return;
            Console.WriteLine("==> Starting monitoring CardReader.");
            try
            {
                string[] readers = card.GetReaders();
                if (readers.Length != 0)
                {
                    for (int i = 0; i < readers.Length; i++)
                    {
                        card.MonitorStart(readers[i]);
                    }
                }
                card.eventCardInsertedWithPhoto += OnCardInsertedWithPhoto;
                card.eventCardRemoved += OnCardRemoved;
                isMonitoringReader = true;
                Console.WriteLine("==> Started monitoring CardReader.");
            }
            catch (PCSCException e)
            {
                Console.WriteLine("==> card error: {0}", e.Message);
                SendError(Messages.CARD_READER_NOT_CONNECTED);
            }

        }

        private void StopWatchingCardReader()
        {
            if (!isMonitoringReader) return;
            Console.WriteLine("==> Stopping Card reader");
            try
            {
                string[] readers = card.GetReaders();
                if (readers.Length != 0)
                {
                    for (int i = 0; i < readers.Length; i++)
                    {
                        card.MonitorStop(readers[i]);
                    }
                }
            }
            catch (PCSCException e)
            {
                Console.WriteLine("==> Error while stopping card reader : {0}", e.Message);
            }
            finally
            {
                card.eventCardInsertedWithPhoto -= OnCardInsertedWithPhoto;
                card.eventCardRemoved -= OnCardRemoved;
                isMonitoringReader = false;
                Console.WriteLine("==> Card reader stopped");
            }
        }
        private void OnCardInsertedWithPhoto(Personal e)
        {
            Console.WriteLine("==> Card inserted");
            SendStatus(Messages.CARD_INSERTED);

            Personal personal = e;
            if (personal != null)
            {
                String string_photo = Convert.ToBase64String(personal.PhotoRaw);

                var data = new Dictionary<String, dynamic>();
                data.Add("type", "data");
                data.Add("cid", personal.Citizenid);
                data.Add("birthday", personal.Birthday?.ToString("dd/MM/yyyy")??"");
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

                SendData(data);
            }

        }

        private void OnCardRemoved()
        {

            Console.WriteLine("==> Card removed");
            SendStatus(Messages.CARD_REMOVED);
        }

        private Dictionary<String, dynamic>? ReadPersonalData(bool withPhoto = true)
        {
            var personal = card.readAll(withPhoto);
            if (personal == null)
            {
                return null;
            }
            else
            {
                Dictionary<String, dynamic> ps = new Dictionary<String, dynamic>
                        {
                            { "cid", personal.Citizenid },
                            { "birthday", personal.Birthday?.ToString("dd/MM/yyyy")??"" },
                            { "sex", personal.Sex },
                            { "th_prefix", personal.Th_Prefix },
                            { "th_firstname", personal.Th_Firstname },
                            { "th_lastname", personal.Th_Lastname },
                            { "en_prefix", personal.En_Prefix },
                            { "en_firstname", personal.En_Firstname },
                            { "en_lastname", personal.En_Lastname },
                            { "issue", personal.Issue.ToString("dd/MM/yyyy") },
                            { "expire", personal.Expire.ToString("dd/MM/yyyy") },
                            { "address", personal.Address },
                            { "addr_house_no", personal.addrHouseNo },
                            { "addr_village_no", personal.addrVillageNo },
                            { "addr_lane", personal.addrLane },
                            { "addr_road", personal.addrRoad },
                            { "addr_tambol", personal.addrTambol },
                            { "addr_amphur", personal.addrAmphur },
                            { "addr_province", personal.addrProvince },
                            { "photo", Convert.ToBase64String(personal.PhotoRaw) }
                        };

                return ps;
            }
        }
        // ----------

        // Messaging method
        public void SendStatus(string status)
        {
            Console.WriteLine("==> SendStatus : {0}", status);
            SendMessageToClient(MessageUtil.status(status));
        }
        public void SendDevice(string[]? readers = null )
        {
            if (readers == null) {
                try
                {
                    readers = card.GetReaders();
                } catch
                {
                    readers = Array.Empty<string>();
                }
                
            }
            SendMessageToClient(MessageUtil.devices(readers));
        }
        public void SendMessage(String message)
        {
            Console.WriteLine("==> SendMessage : {0}", message);
            SendMessageToClient(MessageUtil.message(message));
        }
        public void SendData(Dictionary<string, dynamic> data)
        {
            Console.WriteLine("==> SendData : {0}", data);
            SendMessageToClient(MessageUtil.dictionaryToJson(data));
        }
        public void SendError(String error)
        {
            Console.WriteLine("==> SendError : {0}", error);
            SendMessageToClient(MessageUtil.error(error));
        }

        public void SendMessageToClient(string msg)
        {
            if (connectedClientStream == null) return;
            Console.WriteLine("==> sending message to client : {0}", msg.Length>100? msg.Substring(0,100) + "..." : msg);
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
                    if (!connectedClientStream.CanWrite) return;
                    connectedClientStream.Write(IntToByteArray((ushort)header), 0, 2);
                    connectedClientStream.Write(list, 0, list.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine("==> SendMessageToClient error: {0} \n {1}", e.Message, e.StackTrace);
                }

            }
        }
        private static int GetHeader(bool finalFrame, bool contFrame)
        {
            int header = finalFrame ? 1 : 0; //fin: 0 = more frames, 1 = final frame
            header = (header << 1) + 0; //rsv1
            header = (header << 1) + 0; //rsv2
            header = (header << 1) + 0; //rsv3
            header = (header << 4) + (contFrame ? 0 : 1); //opcode : 0 = continuation frame, 1 = text
            header = (header << 1) + 0; //mask: server -> client = no mask

            return header;
        }

        private static byte[] IntToByteArray(ushort value)
        {
            var ary = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ary);
            }

            return ary;
        }
        // ----------

    }
}

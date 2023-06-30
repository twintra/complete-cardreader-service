//
// csc wsserver.cs
// wsserver.exe

using PCSC.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TestServer;
using ThaiNationalIDCard;

class Program
{

    public static void Main()
    {
        string ip = "127.0.0.1";
        int port = 7023;
        SocketServerController server = new SocketServerController(ip, port);
        server.Start();
        Console.WriteLine("Service stopped.");
    }

}

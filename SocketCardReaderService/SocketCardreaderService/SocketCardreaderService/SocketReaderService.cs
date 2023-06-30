using PCSC.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThaiNationalIDCard;

namespace SocketCardreaderService
{
    public partial class SocketReaderService : ServiceBase
    {
        SocketServerController server;
        public SocketReaderService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread thread = new Thread(InitServer);
            thread.Start();
        }

        public void InitServer()
        {
            string ip = "127.0.0.1";
            int port = 7023;
            server = new SocketServerController(ip, port);
            server.Start();

        }

        protected override void OnStop()
        {
            server.Stop();
        }

        
    }
}

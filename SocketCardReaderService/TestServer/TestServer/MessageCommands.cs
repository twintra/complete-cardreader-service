using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    internal class MessageCommands
    {
        public static string STATUS = "status";
        public static string CARD_DATA = "cardData";
        public static string DEVICES = "devices";
        public static string IS_MONITORING = "isMonitoring";
        public static string START_MONITORING = "startMonitoringReader";
        public static string STOP_MONITORING = "stopMonitoringReader";
        public static string DISCONNECT = "disconnect";

        
        public static List<string> commandsList()
        {
            List<string> commands = new List<string>();
            commands.Add(STATUS);
            commands.Add(CARD_DATA);
            commands.Add(DEVICES);
            commands.Add(IS_MONITORING);
            commands.Add(START_MONITORING);
            commands.Add(STOP_MONITORING);
            commands.Add(DISCONNECT);
            return commands;


        }

    }
}

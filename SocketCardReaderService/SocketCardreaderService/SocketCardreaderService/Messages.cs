using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketCardreaderService
{
    internal class Messages
    {
        public static string CARD_INSERTED = "card_inserted";
        public static string CARD_REMOVED = "card_removed";
        public static string SUCCESS = "success";
        public static string ERROR = "error";
        public static string CARD_READER_NOT_CONNECTED = "reader_not_connected";
    }
}

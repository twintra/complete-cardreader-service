using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThaiNationalIDCard;

namespace TestServer
{
    class MessageUtil
    {
        public static string status(string sts)
        {
            Dictionary<string, dynamic> json = new Dictionary<string, dynamic>();
            json.Add("type", "status");
            json.Add("data", sts);
            return dictionaryToJson(json);
        }
        public static string devices(string[] devices)
        {
            Dictionary<string, dynamic> json = new Dictionary<string, dynamic>();
            json.Add("type", "devices");
            json.Add("data","["+ string.Join(",",devices.ToArray())+"]");
            return dictionaryToJson(json);
        }
        public static string message(string msg)
        {
            Dictionary<string, dynamic> json = new Dictionary<string, dynamic>();
            json.Add("type", "message");
            json.Add("data", msg);
            return dictionaryToJson(json);
        }
        public static string data(Dictionary<string, dynamic> data)
        {
            Dictionary<string, dynamic> json = new Dictionary<string, dynamic>();
            json.Add("type", "data");
            json.Add("data", data);
            return dictionaryToJson(json);
        }
        public static string error(string msg)
        {
            Dictionary<string, dynamic> json = new Dictionary<string, dynamic>();
            json.Add("type", "error");
            json.Add("data", msg);
            return dictionaryToJson(json);
        }


        public static string dictionaryToJson(Dictionary<string, dynamic> data)
        {
            var entries = data.Select(d => {
                if (d.Value.GetType() == typeof(Dictionary<string, dynamic>))
                {
                    return dictionaryToJson(d.Value);
                }
                else if (d.Value.GetType() == typeof(bool))
                {
                    return string.Format("\"{0}\": {1}", d.Key, d.Value);
                }
                else if (d.Value.GetType() == typeof(int))
                {
                    return string.Format("\"{0}\": {1}", d.Key, d.Value);
                }
                else
                {
                    return string.Format("\"{0}\": \"{1}\"", d.Key, string.Join(",", getUnicodeValue(d.Value)));
                }
                
            });
            return "{" + string.Join(",", entries) + "}";
        }

        private static string getUnicodeValue(string value)
        {
            if (System.Text.Encoding.UTF8.GetByteCount(value) != value.Length)
            {
                string result = "";
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                for (int i = 0; i < value.Length; i++)
                {
                    result += String.Format("\\u{0:x4}", (int)value[i]);
                }
                return result;
            }
            else return value;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.CryptoCortex.Models;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests
{
    public class Util
    {
        public static string paramsFolder = @"D:\MarketMaker\params\";
        public const double delta = 1e-10;
        public const double bps = 1e-4;
        public static string ReadFile(string fileName)
        {
            string line = "";
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    line = sr.ReadToEnd().Trim(' ', '\n', '\r', (char)26);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return line;
        }

        public static bool CompareDouble(double first, double second)
        {
            //Debug.WriteLine("First: {0}, Second: {1}", first, second);
            return Math.Abs(first - second) < delta;
        }

        public static string GetSendOrderRequest(OrderDbo order)
        {
            return String.Format("correlation-id:ioeswd7t9m\r\nX-Deltix-Nonce:{0}\r\ndestination:/app/v1/orders/create\r\n\r\n{1}",
                                 StompWebSocketService.ConvertToUnixTimestamp(DateTime.Now),
                                 JsonConvert.SerializeObject(order));
        }
    }
}

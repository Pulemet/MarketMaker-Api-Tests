using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketMaker_Api_Tests
{
    public class Util
    {
        public static string paramsFolder = @"D:\MarketMaker\params\";
        public static double delta = 1e-10;
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
            return Math.Abs(first - second) < delta;
        }
    }
}

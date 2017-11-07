using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NinjaTrader.Data
{
    public class GLog
    {
        public static void Print(string message)
        {
            File.AppendAllText(@"c:\nt\log.txt", DateTime.Now.ToString("yyyy-MM-dd") + "  I  " + message + "\n");
        }
    }
}
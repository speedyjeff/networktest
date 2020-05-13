using System;
using System.Diagnostics;
using System.Threading;

namespace Client
{
    class Program
    {
        public static int Main(string[] args)
        {
            var url = args.Length > 0 ? args[0] : "http://dl.google.com/googletalk/googletalk-setup.exe";
            if (args.Length == 0)
            {
                Console.WriteLine("./client <address>");
                Console.WriteLine($"    using default {url}");
            }

            // init
            var timer = new Stopwatch();
            byte[] data;

            // start the watching thread
            Reader = new Thread(WaitForConsoleInput);
            Reader.Start();

            while (System.Threading.Volatile.Read(ref IsSet) == 0)
            {
                // get content
                using (var client = new System.Net.WebClient())
                {
                    timer.Start();
                    data = client.DownloadData(url);
                    timer.Stop();
                }

                // calculate
                var speed = ((double)data.LongLength / timer.Elapsed.TotalSeconds) / (1024d*1024d);

                // output
                Console.WriteLine($"{DateTime.Now:o}\t{timer.Elapsed}\t{speed:f2}");

                timer.Reset();
            }

            return 0;
        }

        #region private
        private static Thread Reader;
        private static int IsSet;

        private static void WaitForConsoleInput()
        {
            // wait for input
            Console.ReadLine();

            // set that input was received
            System.Threading.Volatile.Write(ref IsSet, 1);
        }
        #endregion
    }
}

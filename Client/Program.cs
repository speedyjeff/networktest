using System;
using System.Diagnostics;
using System.Threading;

namespace Client
{
    class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("./client <address>");
                return 0;
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
                    data = client.DownloadData(args[0]);
                    timer.Stop();
                }

                // calculate
                var speed = ((double)data.LongLength / timer.Elapsed.TotalSeconds) / (1024d*1024d);

                // output
                Console.WriteLine($"{timer.Elapsed} {speed:N0} mbps");

                timer.Reset();
            }

            return 0;
        }


        #region private
        private static Thread Reader;
        private static int IsSet;

        private static void WaitForConsoleInput()
        {
            while (true)
            {
                // wait for input
                Console.ReadLine();

                // set that input was received
                System.Threading.Volatile.Write(ref IsSet, 1);
            }
        }
            #endregion
    }
}

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Client
{
    class WebClientWithTimeout : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = 5000; // timeout in milliseconds (ms)
            return wr;
        }
    }

    class Program
    {
        public static int Main(string[] args)
        {
            var url = args.Length > 0 ? args[0] : "http://dl.google.com/googletalk/googletalk-setup.exe";
            var stopAfter = args.Length > 1 ? Convert.ToInt32(args[1]) : -1;
            if (args.Length == 0)
            {
                Console.WriteLine("./client <address> <stop after ms>");
                Console.WriteLine($"    using default {url}");
            }

            // init
            var timer = new Stopwatch();
            var duration = new Stopwatch();
            byte[] data;

            // start the watching thread
            Reader = new Thread(WaitForConsoleInput);
            Reader.Start();

            duration.Start();
            while (System.Threading.Volatile.Read(ref IsSet) == 0)
            {
	        try
                {
                    // get content
                    using (var client = new WebClientWithTimeout())
                    {
                        timer.Start();
                        data = client.DownloadData(url);
                        timer.Stop();
                    }

                    // calculate
                    var speed = ((double)data.LongLength / timer.Elapsed.TotalSeconds) / (1024d*1024d);

                    // output
                    Console.WriteLine($"{DateTime.Now:o}\t{timer.ElapsedMilliseconds}\t{speed:f2}");
                }
                catch(Exception e)
                {
                    timer.Stop();
                    Console.WriteLine($"{DateTime.Now:o}\t{timer.ElapsedMilliseconds}\t-1\t{e.Message}");
                }

                timer.Reset();

                // check if we should stop
                if (stopAfter > 0 && duration.ElapsedMilliseconds > stopAfter)
                {
                    break;
                }
            }

            Environment.Exit(0);
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

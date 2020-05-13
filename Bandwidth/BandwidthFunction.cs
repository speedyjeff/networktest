using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bandwidth
{
    public static class BandwidthFunction
    {
        [FunctionName("Bandwidth")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // fill the buffer with garbage
            if (Buffer == null)
            {
                var rand = new Random();
                Buffer = new byte[1606064]; // ~1.5 mb
                for (int i = 0; i < Buffer.Length; i++) Buffer[i] = (byte)(rand.Next() % 256);
            }

            return new OkObjectResult(Buffer);
        }

        #region private
        private static byte[] Buffer;
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace JoshuaKearney.HttpHelper.Testing {
    class MessageResult {
        public string Html { get; set; }
        public int Code { get; set; }
    }

    class Program {
        static void Main(string[] args) {
            Run().GetAwaiter().GetResult();
        }

        static async Task Run() {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://github.com");

            var reader = new HttpResponseReader<MessageResult>();

            reader.UseBrotliDecompression();
            reader.EnsureHtmlContent((x, result) => result.Html = x);
            reader.UseResponseCode((x, result) => result.Code = (int)x);

            MessageResult res = new MessageResult();
            if (await reader.TryReadMessageAsync(response, res)) {
                Console.WriteLine(res.Html);
                Console.WriteLine("Status code: " + res.Code);
            }

            Console.Read();
        }        
    }
}
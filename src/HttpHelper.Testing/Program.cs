using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace JoshuaKearney.HttpHelper.Testing {
    class Program {
        static void Main(string[] args) {
            Run().GetAwaiter().GetResult();
        }

        static async Task Run() {
            string url = "http://www.youtube.com";

            HttpClient client = new HttpClient(new HttpClientHandler() {
                AutomaticDecompression = DecompressionMethods.None
            });

            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.TryAddWithoutValidation("accept-encoding", "gzip, deflate");

            var response = await client.GetAsync(url);

            HttpResponseReader reader = new HttpResponseReader();

            //reader.UseBrotliDecompression();
            reader.UseDeflateDecompression();
            reader.UseGzipDecompression();

            reader.EnsureHtmlContent(html => Console.WriteLine(html));

            Console.WriteLine(await reader.TryReadMessageAsync(response));
            Console.Read();
        }        
    }
}
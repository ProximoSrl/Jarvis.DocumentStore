// See https://aka.ms/new-console-template for more information
using Jarvis.DocumentStore.Client;
using System;

namespace Jarvis.Shell
{
    internal static class Program
    {
        private static async Task<Int32> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello, World!");

                await TryToDownloadFile("C:\\temp\\out1.mp4");
                await TryToDownloadFileRange("C:\\temp\\out2.mp4", 0, 100000);
                await TryToDownloadFileRange("C:\\temp\\out3.mp4", 0, null);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.ToString());
            }

            return 0;
        }

        private static async Task TryToDownloadFile(string outFileName)
        {
            if (File.Exists(outFileName))
            {
                File.Delete(outFileName);
            }
            var docClient = new DocumentStoreServiceClient(new Uri("http://localhost:5123"), "docs", new TestHttpClientFactory());
            var doc = docClient.OpenRead(new Jarvis.DocumentStore.Client.Model.DocumentHandle("blob_10"));

            var stream = await doc.OpenStream();

            Console.Write(stream.GetType().FullName);
            using (var f = new FileStream(outFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.CopyTo(f);
            }
        }

        private static async Task TryToDownloadFileRange(string outFileName, long from, long? end)
        {
            if (File.Exists(outFileName))
            {
                File.Delete(outFileName);
            }
            var docClient = new DocumentStoreServiceClient(new Uri("http://localhost:5123"), "docs", new TestHttpClientFactory());

            OpenOptions options = new OpenOptions
            {
                SkipContent = false,
            };

            options.RangeFrom = from;
            options.RangeTo = end;

            var doc = docClient.OpenRead(new Jarvis.DocumentStore.Client.Model.DocumentHandle("blob_10"), options: options);

            var stream = await doc.OpenStream();

            Console.Write(stream.GetType().FullName);
            using (var f = new FileStream(outFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.CopyTo(f);
            }
        }
    }

    public class TestHttpClientFactory : IHttpClientFactory
    {
        private static HttpClient _httpClient = new HttpClient();
        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }
    }
}
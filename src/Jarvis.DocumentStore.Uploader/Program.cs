using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;

namespace Jarvis.DocumentStore.Uploader
{
    class Program
    {
        static string _folder;
        static Uri _endPoint;
        static int Main(string[] args)
        {
            if (!Parse(args))
            {
                PrintUsage();
                return -1;
            }


            //_folder = @"C:\dev\Jarvis.ImageService\src\Jarvis.ImageService.Core.Tests\Docs";
            //_endPoint = new Uri("http://localhost:5123");
            
            return Run();
        }

        static bool Parse(string[] args)
        {
            if (!args.Any())
                return false;

            string server = null;

            for (int index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg.ToLowerInvariant())
                {
                    case "--server":
                        server = args[++index];
                        break;

                    case "--folder":
                        _folder = args[++index];
                        break;
                }
            }


            if (server == null)
                return false;

            if (_folder == null)
                return false;

            _endPoint = new Uri(server);
            return true;
        }

        static int Run()
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Running");
            Console.WriteLine("\tServer: {0}", _endPoint.AbsoluteUri);
            Console.WriteLine("\tFolder: {0}", _folder);
            Console.WriteLine("------------------------------------------");

            var client = new DocumentStoreServiceClient(_endPoint);
            int counter = 1;

            Parallel.ForEach(Directory.GetFiles(_folder), file =>
            {
                Console.WriteLine("Processing file {0}", file);
                var id = Interlocked.Increment(ref counter);
                try
                {
                    client.Upload(file, "File_" + id).Wait();
                }
                catch (AggregateException aex)
                {
                    foreach(var ex in aex.InnerExceptions)
                        Console.WriteLine("Error: {0}", ex.Message);
                }
            });

            Console.WriteLine("------------------------------------------");

            return 0;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: Jarvis.ImageService.Uploader --host hostname --folder folder");
            Console.WriteLine();
        }
    }
}

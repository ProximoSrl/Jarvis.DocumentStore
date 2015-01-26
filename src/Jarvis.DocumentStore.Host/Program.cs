using System;
using System.Configuration;
using System.Linq;
using CQRS.Kernel.ProjectionEngine.Client;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;
using Topshelf;
using Jarvis.ConfigurationService.Client;
using System.Threading;

namespace Jarvis.DocumentStore.Host
{
    class Program
    {
        static DocumentStoreConfiguration _documentStoreConfiguration;

        static int Main(string[] args)
        {
            Int32 exitCode;
            if (args.Length > 0)
            {
                //TEMP: Single process executor run
                String dsBaseAddress = FindArgument(args, "/dsuris:");
                String queueName = FindArgument(args, "/queue:");
                if (String.IsNullOrEmpty(dsBaseAddress) || String.IsNullOrEmpty(queueName))
                {
                    Console.WriteLine("Error in parameters: dsuris={0} queue={1}", dsBaseAddress, queueName);
                    Console.ReadKey();
                    return -1;
                }
                  
                exitCode = SingleJobStart(dsBaseAddress, queueName);
            }
            else
            {
                exitCode = (Int32) StandardDocumentStoreStart();
            }
            return (int)exitCode;
        }

        private static TopshelfExitCode StandardDocumentStoreStart()
        {
            SetupColors();

            LoadConfiguration();

            ConfigureRebuild();

            var exitCode = HostFactory.Run(host =>
            {
                var resourceDownload = ConfigurationServiceClient.Instance.DownloadResource("log4net.config", monitorForChange: true);
                if (!resourceDownload)
                {
                    Console.Error.WriteLine("Unable to download log4net.config from configuration store");
                }

                host.UseOldLog4Net("log4net.config");

                host.Service<DocumentStoreBootstrapper>(service =>
                {
                    var uri = new Uri(ConfigurationManager.AppSettings["endPoint"]);
                    service.ConstructUsing(() => new DocumentStoreBootstrapper());
                    service.WhenStarted(s => s.Start(_documentStoreConfiguration));
                    service.WhenStopped(s => s.Stop());
                });

                host.RunAsNetworkService();

                host.SetDescription("Jarvis - Document Store");
                host.SetDisplayName("Jarvis - Document Store");
                host.SetServiceName("JarvisDocumentStore");
            });
            return exitCode;
        }

        private static Int32 SingleJobStart(String dsBaseAddress, String queueName)
        {
            //Avoid all sub process to start at the same moment.
            Thread.Sleep(new Random().Next(1000, 3000));
            Console.WindowWidth = 140;
            SetupColors();

            LoadConfiguration();

            try
            {
                var resourceDownload = ConfigurationServiceClient.Instance.DownloadResource("log4net.config", monitorForChange: true);
                if (!resourceDownload)
                {
                    Console.Error.WriteLine("Unable to download log4net.config from configuration store");
                }
            }
            catch (System.IO.IOException ex)
            {
                //If multiple prcesses starts, we cannot access log4net.config because it can be lcoked.
            }
       
            var uri = new Uri(ConfigurationManager.AppSettings["endPoint"]);
            var bootstrapper = new DocumentStoreSingleQueueClientBootstrapper(uri, queueName);
            var jobStarted = bootstrapper.Start(_documentStoreConfiguration);

            if (jobStarted)
            {
                Console.Title = "Jarvis - Document Store Client for queue " + queueName;
                Console.WriteLine("JOB STARTED: Press any key to stop the client");
                Console.ReadKey(); 
            }
            else
            {
                Console.WriteLine("NO JOB STARTED!!!! CLOSING!!!!");
                Thread.Sleep(3000);
                return -1; //code to signal that queue is not supported.
            }
            return 0;
        }

        static void SetupColors()
        {
            if (!Environment.UserInteractive)
                return;
            Console.Title = "JARVIS :: Document Store Service";
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Clear();
        }

        static void LoadConfiguration()
        {
            var roles = ConfigurationManager.AppSettings["roles"];
            bool isApiServer = false;
            bool isWorker = false;
            bool isReadmodelBuilder = false;

            if (roles != null)
            {
                var roleArray = roles.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToArray();

                isApiServer = roleArray.Contains("api");
                isWorker = roleArray.Contains("worker");
                isReadmodelBuilder = roleArray.Contains("projections");
            }

            _documentStoreConfiguration = new RemoteDocumentStoreConfiguration();
        }

        static void ConfigureRebuild()
        {
            if (!Environment.UserInteractive)
                return;

            if (!_documentStoreConfiguration.IsReadmodelBuilder)
                return;

            Banner();

            RebuildSettings.Init(
                ConfigurationManager.AppSettings["rebuild"] == "true",
                ConfigurationManager.AppSettings["nitro-mode"] == "true"
            );

            if (RebuildSettings.ShouldRebuild)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("---> Rebuild the readmodel (y/N)?");

                var res = Console.ReadLine().Trim().ToLowerInvariant();
                if (res != "y")
                {
                    RebuildSettings.DisableRebuild();
                }
            }
        }

        private static string FindArgument(string[] args, string prefix)
        {
            var arg = args.FirstOrDefault(a => a.StartsWith(prefix));
            if (String.IsNullOrEmpty(arg)) return String.Empty;
            return arg.Substring(prefix.Length);
        }

        private static void Banner()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===================================================================");
            Console.WriteLine("Jarvis Document Store - Proximo srl");
            Console.WriteLine("===================================================================");
            Console.WriteLine("  install                        -> install service");
            Console.WriteLine("  uninstall                      -> remove service");
            Console.WriteLine("  net start JarvisDocumentStore  -> start service");
            Console.WriteLine("  net stop JarvisDocumentStore   -> stop service");
            Console.WriteLine("===================================================================");
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}

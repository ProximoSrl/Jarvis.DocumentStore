using System;
using System.Configuration;
using System.Linq;
using CQRS.Kernel.ProjectionEngine.Client;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;
using Topshelf;
using Jarvis.ConfigurationService.Client;
using System.Threading;
using System.Diagnostics;

namespace Jarvis.DocumentStore.Host
{
    class Program
    {
        static DocumentStoreConfiguration _documentStoreConfiguration;

        static int Main(string[] args)
        {
            if (args.Length > 0) return -1;
            MongoFlatMapper.EnableFlatMapping(); //before any chanche that the driver scan any type.
            var executionExitCode = StandardDocumentStoreStart();
            return (Int32)executionExitCode;
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

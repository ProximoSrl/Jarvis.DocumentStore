using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Shared.Helpers;
using Jarvis.Framework.Kernel.ProjectionEngine.Client;
using Jarvis.Framework.UdpAppender;
using log4net.Core;
using Topshelf;
using Jarvis.ConfigurationService.Client;
using System.Threading;
using System.Diagnostics;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Host
{
    class Program
    {
        static DocumentStoreConfiguration _documentStoreConfiguration;

        static int Main(string[] args)
        {
            
            try
            {
                CommandsExtensions.EnableDiagnostics = true;
                Native.DisableWindowsErrorReporting();
                MongoFlatMapper.EnableFlatMapping(); //before any chanche that the driver scan any type.
                var executionExitCode = StandardDocumentStoreStart();
                return (Int32)executionExitCode;
            }
            catch (Exception ex)
            {
                var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_lastError.txt");
                File.WriteAllText(fileName, ex.ToString());
                throw;
            }
          
        }

        private static TopshelfExitCode StandardDocumentStoreStart()
        {
            SetupColors();

            LoadConfiguration();

            ConfigureRebuild(_documentStoreConfiguration);

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
            _documentStoreConfiguration = new RemoteDocumentStoreConfiguration();
        }

        static void ConfigureRebuild(DocumentStoreConfiguration config)
        {
            if (!Environment.UserInteractive)
                return;

            if (!_documentStoreConfiguration.IsReadmodelBuilder)
                return;

            Banner();

            RebuildSettings.Init(config.Rebuild, config.NitroMode);

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

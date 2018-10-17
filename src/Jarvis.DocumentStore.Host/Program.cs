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
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Kernel.Support;

namespace Jarvis.DocumentStore.Host
{
    public static class Program
    {
        private static DocumentStoreConfiguration _documentStoreConfiguration;

        private static ILogger _logger;

        static int Main(string[] args)
        {
            var lastErrorFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_lastError.txt");
            if (File.Exists(lastErrorFileName)) File.Delete(lastErrorFileName);
            try
            {
                MongoRegistration.RegisterMongoConversions(
                    "NEventStore.Persistence.MongoDB"
                    );

                MongoFlatMapper.EnableFlatMapping(true);

                CommandsExtensions.EnableDiagnostics = true;
                Native.DisableWindowsErrorReporting();
                MongoFlatMapper.EnableFlatMapping(true); //before any chanche that the driver scan any type.
                Int32 executionExitCode;
                if (args.Length == 1 && (args[0] == "install" || args[0] == "uninstall"))
                {
                    executionExitCode = (Int32) StartForInstallOrUninstall();
                }
                else
                {
                    executionExitCode = (Int32) StandardDocumentStoreStart();
                }
                return executionExitCode;
            }
            catch (Exception ex)
            {
                File.WriteAllText(lastErrorFileName, ex.ToString());
                throw;
            }
        }

        private static TopshelfExitCode StartForInstallOrUninstall()
        {
            var exitCode = HostFactory.Run(host =>
            {
                host.Service<Object>(service =>
                {
                    service.ConstructUsing(() => new Object());
                    service.WhenStarted(s => Console.WriteLine("Start fake for install"));
                    service.WhenStopped(s => Console.WriteLine("Stop fake for install"));
                });

                host.RunAsNetworkService();

                host.SetDescription("Jarvis - Document Store");
                host.SetDisplayName("Jarvis - Document Store");
                host.SetServiceName("JarvisDocumentStore");
            });
            return exitCode;
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

            if (Environment.UserInteractive && exitCode != TopshelfExitCode.Ok)
            {
                Console.Error.WriteLine("Abnormal exit from topshelf: {0}. Press a key to continue", exitCode);
                Console.ReadKey();
            }
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

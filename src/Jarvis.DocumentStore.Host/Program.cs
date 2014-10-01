using System;
using System.Configuration;
using CQRS.Kernel.ProjectionEngine.Client;
using Jarvis.DocumentStore.Host.Support;
using Topshelf;

namespace Jarvis.DocumentStore.Host
{
    class Program
    {
        static int Main(string[] args)
        {
            ConfigureRebuild();

            var exitCode = HostFactory.Run(host =>
            {
                host.UseOldLog4Net("log4net.config");

                host.Service<DocumentStoreBootstrapper>(service =>
                {
                    service.ConstructUsing(() => new DocumentStoreBootstrapper( new Uri("http://localhost:5123")));
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                host.RunAsNetworkService();

                host.SetDescription("Jarvis - Document Store");
                host.SetDisplayName("Jarvis - Document Store");
                host.SetServiceName("JarvisDocumentStore");
            });

            return (int)exitCode;
        }

        static void ConfigureRebuild()
        {
            if (!Environment.UserInteractive)
                return;

            if (!RolesHelper.IsReadmodelBuilder)
                return;

            Console.Title = "JARVIS :: Document Store Service";
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Clear();
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

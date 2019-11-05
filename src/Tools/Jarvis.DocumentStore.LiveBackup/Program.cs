using Castle.Core.Logging;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.LiveBackup.Support;
using MongoDB.Bson.Serialization;
using System;
using System.Configuration;
using System.IO;
using Topshelf;

namespace Jarvis.DocumentStore.LiveBackup
{
    internal class Program
    {
        private static ILogger _logger = NullLogger.Instance;

        private const string ServiceDescriptiveName = "Jarvis - Documentstore LiveBackup";
        private const string ServiceName = "Documentstore.LiveBackup";
        private static string lastErrorFileName;

        internal static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        private static void Main(string[] args)
        {
            //Preload class needed by mongo
            BsonClassMap.LookupClassMap(typeof(BlobId));
            BsonClassMap.LookupClassMap(typeof(DocumentHandle));

            BsonClassMap.RegisterClassMap<FileNameWithExtension>(m =>
            {
                m.AutoMap();
                m.MapProperty(x => x.FileName).SetElementName("name");
                m.MapProperty(x => x.Extension).SetElementName("ext");
            });

            ConfigurationServiceClient.AppDomainInitializer(
               (message, isError, exception) =>
               {
                   if (isError) Console.Error.WriteLine(message + "\n" + exception);
                   else Console.WriteLine(message);
               },
               "JARVIS_CONFIG_SERVICE",
               null,
               new FileInfo("defaultParameters.config"),
               missingParametersAction: ConfigurationManagerMissingParametersAction.Blank);

            lastErrorFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_lastError.txt");
            if (args.Length == 1)
            {
                if (args[0] == "install" || args[0] == "uninstall")
                {
                    var runAsSystem = "true".Equals(
                       ConfigurationManager.AppSettings["runs-as-system"],
                           StringComparison.OrdinalIgnoreCase);
                    var dependencies = ConfigurationManager.AppSettings["depends-on-services"] ?? "";

                    ServiceInstallHelper.StartForInstallOrUninstall(
                        runAsSystem, dependencies, ServiceDescriptiveName, ServiceName);
                }
                else if (args[0] == "restore")
                {
                    //Bootstrapper bs = new Bootstrapper();
                    //bs.Start(false);
                    //var restoreJob = bs.GetRestoreJob();
                    //restoreJob.Start();
                }
            }
            else
            {
                var exitcode = StandardStart();
                if (Environment.UserInteractive)
                {
                    if (exitcode != TopshelfExitCode.Ok && Environment.UserInteractive)
                    {
                        Console.WriteLine("Failure exit code: {0} press a key to continue", exitcode.ToString());
                        Console.ReadKey();
                    }
                }
            }
        }

        private static TopshelfExitCode StandardStart()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.Title = ServiceDescriptiveName;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.Clear();
                    Banner();
                }

                return HostFactory.Run(x =>
                {
                    x.UseLog4Net("log4net.config");
                    x.Service<Bootstrapper>(s =>
                    {
                        s.ConstructUsing(name => new Bootstrapper());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });
                    x.RunAsLocalSystem();

                    x.SetDescription(ServiceDescriptiveName);
                    x.SetDisplayName(ServiceDescriptiveName);
                    x.SetServiceName(ServiceName);
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(lastErrorFileName, ex.ToString());
                throw;
            }
        }

        private static void Banner()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===================================================================");
            Console.WriteLine("Jarvis Documentstore LiveBackup - Proximo srl");
            Console.WriteLine("===================================================================");
            Console.WriteLine("  install                             -> Installa il servizio");
            Console.WriteLine("  uninstall                           -> Rimuove il servizio");
            Console.WriteLine("  net start Documentstore.LiveBackup  -> Avvia il servizio");
            Console.WriteLine("  net stop Documentstore.LiveBackup   -> Arresta il servizio");
            Console.WriteLine("===================================================================");
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}

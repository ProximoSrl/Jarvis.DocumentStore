﻿using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Windsor;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Shell.BlobStoreSync;
using Jarvis.DocumentStore.Tools.Helpers;
using Jarvis.Framework.Shared.IdentitySupport;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jarvis.DocumentStore.Tools
{
    internal static class Program
    {
        private static Int32 Main(string[] args)
        {
            //Premap everything you need to load.
            BsonClassMap.LookupClassMap(typeof(BlobId));
            BsonClassMap.LookupClassMap(typeof(DocumentHandle));

            BsonClassMap.RegisterClassMap<FileNameWithExtension>(m =>
            {
                m.AutoMap();
                m.MapProperty(x => x.FileName).SetElementName("name");
                m.MapProperty(x => x.Extension).SetElementName("ext");
            });

            try
            {
                RunShell();
                return 0;
            }
            catch (ReflectionTypeLoadException rex)
            {
                StringBuilder error = new StringBuilder();

                error.AppendLine(@"
------------------------------------------------------------------------------
                UNABLE TO LOAD TYPES

One of the standard reason why this happens is that in jarvis.shell.exe.config
in runtime section redirection for Newtonsoft is missing. To fix the problem
if the error is newtonsoft, check the version of newtonsoft.dll that is present
in the directory, then fix accordingly the config file, inserting the right 
version in runtime section.

As example, if the exeption tells you that newtonsoft 6.0.0 is missing, and
the dll in the directory is version 8.0.0. you probably have this in the config

<dependentAssembly>
    <assemblyIdentity name=""Newtonsoft.Json"" publicKeyToken=""30ad4fe6b2a6aeed"" culture=""neutral"" />
    <bindingRedirect oldVersion=""0.0.0.0-6.0.0.0"" newVersion=""6.0.0.0"" />
</dependentAssembly>

this should be changed to 
<dependentAssembly>
    <assemblyIdentity name=""Newtonsoft.Json"" publicKeyToken=""30ad4fe6b2a6aeed"" culture=""neutral"" />
    <bindingRedirect oldVersion=""0.0.0.0-8.0.0.0"" newVersion=""8.0.0.0"" />
</dependentAssembly>

------------------------------------------------------------------------------
");
                foreach (var ex in rex.LoaderExceptions)
                {
                    error.AppendLine("LOADER EXCEPTION");
                    error.AppendLine(ex.ToString());
                    error.AppendLine("\n\n");
                }

                error.AppendLine("UNABLE TO LOAD: ");
                foreach (var t in rex.Types.Where(t => t != null))
                {
                    error.AppendLine(t.FullName);
                }

                error
                    .Append("\n\n")
                    .AppendLine(rex.ToString());
                File.WriteAllText("_lasterror.txt", error.ToString());
            }
            catch (Exception ex)
            {
                File.WriteAllText("_lasterror.txt", ex.ToString());
            }

            Console.WriteLine("Error during execution, consult file _lasterror.txt. Press a key to close and open log file.");
            Console.ReadKey();
            Process.Start("_lasterror.txt");
            return 1;
        }

        private static ILoggerFactory _loggerFactory;

        private static void RunShell()
        {
            MongoFlatMapper.EnableFlatMapping();

            if (ConsoleHelper.AskYesNoQuestion("Do you want to use Configuration Service? You should say N if doing tasks like migration that does not require configuration service"))
            {
                ConfigurationServiceClient.AppDomainInitializer(
                    (message, isError, exception) =>
                    {
                        if (isError)
                        {
                            Console.Error.WriteLine(message + "\n" + exception);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                    },
                    "JARVIS_CONFIG_SERVICE",
                    null,
                    new FileInfo("defaultParameters.config"),
                    missingParametersAction: ConfigurationManagerMissingParametersAction.Blank);
            }
            IWindsorContainer _container = new WindsorContainer();
            _container.AddFacility<LoggingFacility>(f => f
                .LogUsing(LoggerImplementation.ExtendedLog4net)
                .WithConfig("log4net.config"));
            _loggerFactory = _container.Resolve<ILoggerFactory>();

            var commands = new Dictionary<String, Func<Boolean>>();
            commands.Add("Check oprhaned blob", () =>
                {
                    CheckOrphanedBlobs.PerformCheck(DateTime.UtcNow);
                    return false;
                });

            commands.Add("Check tika scheduled job", () =>
            {
                CheckQueuedTikaScheduledJobs.PerformCheck();
                return false;
            });

            commands.Add("Start sync artifacts job", () =>
            {
                FullArtifactSyncJob.StartSync();
                return false;
            });

            commands.Add("Copy blob from GridFs to FileSystemFs", () =>
            {
                var startFromBeginning = ConsoleHelper.AskYesNoQuestion("Do you want to start from the beginning of the stream?");
                BlobStoreSync command = new BlobStoreSync(_loggerFactory.Create(typeof(BlobStoreSync)));
                command.SyncAllTenants(BlobStoreType.GridFs, BlobStoreType.FileSystem, startFromBeginning);
                return false;
            });

            commands.Add("Copy blob from FileSystemFs to GridFs", () =>
            {
                var startFromBeginning = ConsoleHelper.AskYesNoQuestion("Do you want to start from the beginning of the stream?");
                BlobStoreSync command = new BlobStoreSync(_loggerFactory.Create(typeof(BlobStoreSync)));
                command.SyncAllTenants(BlobStoreType.FileSystem, BlobStoreType.GridFs, startFromBeginning);
                return false;
            });

            Menu(commands.Keys.ToList());
            CommandLoop(c =>
            {
                int menuSelection = 0;
                if (Int32.TryParse(c, out menuSelection))
                {
                    var func = commands.ElementAtOrDefault(menuSelection);
                    Console.WriteLine("Selected {0}", func.Key);
                    func.Value();
                }
                else
                {
                    if (String.Equals(c, "q", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }

                    Console.WriteLine("Comando non valido! Premere un tasto per continuare");
                    Console.ReadKey();
                }
                Menu(commands.Keys.ToList());
                return false;
            });
        }

        private static void CommandLoop(Func<string, bool> action, String prompt = ">")
        {
            while (true)
            {
                Console.Write(prompt);
                var command = Console.ReadLine();
                if (command == null)
                {
                    continue;
                }

                command = command.ToLowerInvariant().Trim();

                if (action(command))
                {
                    return;
                }
            }
        }

        private static void Banner(string title)
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine(title);
            Console.WriteLine("-----------------------------");
        }

        private static void Menu(List<string> headers)
        {
            Console.Clear();
            Banner("Menu");
            for (int i = 0; i < headers.Count; i++)
            {
                Console.WriteLine("{0} - {1}", i, headers[i]);
            }

            Console.WriteLine("");
            Console.WriteLine("Q - esci");
        }
    }
}

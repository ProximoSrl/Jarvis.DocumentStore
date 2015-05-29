using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Jarvis.DocumentStore.JobsHost.Support;
using Jarvis.DocumentStore.Shared.Helpers;
using Jarvis.Framework.MongoAppender;

namespace Jarvis.DocumentStore.JobsHost
{
    public class Program
    {
        static JobsHostConfiguration _jobsHostConfiguration;

        static int Main(string[] args)
        {
            try
            {
                Native.DisableWindowsErrorReporting();
                Int32 exitCode;
                if (args.Length > 0)
                {
                    //TEMP: Single process executor run
                    String dsBaseAddress = FindArgument(args, "/dsuris:");
                    String queueName = FindArgument(args, "/queue:");
                    String handle = FindArgument(args, "/handle:", "Default");
                    if (String.IsNullOrEmpty(dsBaseAddress) || String.IsNullOrEmpty(queueName))
                    {
                        Console.WriteLine("Error in parameters: dsuris={0} queue={1}", dsBaseAddress, queueName);
                        Console.ReadKey();
                        return -1;
                    }

                    exitCode = SingleJobStart(dsBaseAddress, queueName, handle);
                    return exitCode;
                }

                Console.WriteLine("Wrong command line. This utilities expetcs following parameters:");
                Console.WriteLine("/dsuris:http://localhost:5123 - List of addresses (comma separated) of document store uris.");
                Console.WriteLine("/queue:tika - Name of the queue");
                Console.WriteLine("/handle:xxxxx - Optional, a string that identify this worker");

                Console.ReadKey();
                return 0;
            }
            catch (Exception ex)
            {
                var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_lastError.txt");
                File.WriteAllText(fileName, ex.ToString());
                throw;
            }
           
        }

        private static Int32 SingleJobStart(String dsBaseAddress, String queueName, String handle)
        {
            //Avoid all sub process to start at the same moment.
            Thread.Sleep(new Random().Next(1000, 3000));
            SetupColors();
            LoadConfiguration();
            var uri = new Uri(dsBaseAddress);
            var bootstrapper = new DocumentStoreSingleQueueClientBootstrapper(uri, queueName, handle);
            var jobStarted = bootstrapper.Start(_jobsHostConfiguration);

            if (jobStarted)
            {
                Console.Title = String.Format("Pid {0} - Queue {1} Job Poller Started",
                    Process.GetCurrentProcess().Id, queueName);
                MongoLog.SetProgramName(String.Format("ds-job[Queue:{0}]", queueName));
                Console.WriteLine("JOB STARTED: Press any key to stop the client");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("JOB CANNOT START!!!! CLOSING!!!!");
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

            _jobsHostConfiguration = new JobsHostConfiguration();
        }

        private static string FindArgument(string[] args, string prefix, String defaultValue = "")
        {
            var arg = args.FirstOrDefault(a => a.StartsWith(prefix));
            if (String.IsNullOrEmpty(arg)) return defaultValue;
            return arg.Substring(prefix.Length);
        }

    }
}

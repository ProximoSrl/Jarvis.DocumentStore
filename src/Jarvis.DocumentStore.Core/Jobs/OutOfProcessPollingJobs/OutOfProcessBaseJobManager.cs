using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Castle.Core.Logging;
using Castle.Core;
using System.Management;
using System.Text;
using System.IO;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{


    /// <summary>
    /// Suppose that the process is an external executable with some parameters.
    /// </summary>
    public class OutOfProcessBaseJobManager : IPollerJobManager
    {
        private Boolean _started = false;

        private DocumentStoreConfiguration _configuration;

        private class ProcessInfo
        {
            public Process Process { get; set; }

            public String QueueId { get; set; }

            public List<String> DocStoreAddresses { get; set; }
        }

        Dictionary<String, ProcessInfo> activeProcesses;

        public ILogger Logger { get; set; }

        public OutOfProcessBaseJobManager(DocumentStoreConfiguration configuration)
        {
            _configuration = configuration;
            activeProcesses = new Dictionary<string, ProcessInfo>();
            Logger = NullLogger.Instance;
        }

        public string Start(string queueId, List<string> docStoreAddresses)
        {
            String processHandle = Guid.NewGuid().ToString() + "-" + Environment.MachineName;
            InnerStart(queueId, docStoreAddresses, processHandle);
            _started = true;
            return processHandle;
        }

        private void InnerStart(string queueId, List<string> docStoreAddresses, String processHandle)
        {
            //var thisFileName = Environment.CommandLine.Split(' ')[0]
            //    .Trim('/', '"')
            //    .Replace(".vshost.exe", ".exe");
            var jobsLauncherFileExe = @"JobsRunner\Jarvis.DocumentStore.Jobs.exe";

            Process process = GetLocalProcessForQueue(queueId, jobsLauncherFileExe);
            if (process == null)
            {
                process = new System.Diagnostics.Process();
                process.StartInfo.FileName = jobsLauncherFileExe;
                process.StartInfo.Arguments = "/dsuris:" + docStoreAddresses.Aggregate((s1, s2) => s1 + "|" + s2) +
                    " /queue:" + queueId +
                    " /handle:" + processHandle;

                process.StartInfo.UseShellExecute = true;
                if (_configuration.JobsManagement.WindowVisible && Environment.UserInteractive)
                {
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                }
                else
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                process.StartInfo.RedirectStandardOutput = false;
                process.EnableRaisingEvents = true;
                Boolean started = process.Start();
                Logger.DebugFormat("Started process for queue {0} with ProcessId {1}.", queueId, process.Id);
            }
            else
            {
                Logger.DebugFormat("Reattached process for queue {0} with ProcessId {1}.", queueId, process.Id);
            }
            process.Exited += process_Exited;

            var info = new ProcessInfo()
            {
                Process = process,
                QueueId = queueId,
                DocStoreAddresses = docStoreAddresses,
            };
            activeProcesses[processHandle] = info;
            Logger.InfoFormat("Started worker: ProcessHandle {0} for queue {1}", processHandle, queueId);
        }

        private string GetJobHandleFromProcess(Process process)
        {
            return activeProcesses
                .Where(info => info.Value.Process.Id == process.Id && info.Value.Process.MachineName == process.MachineName)
                .Select(info => info.Key)
                .SingleOrDefault();
        }

        private Process GetLocalProcessForQueue(String queueId, String executableName)
        {
            var processFileName = Path.GetFileNameWithoutExtension(executableName);
            var processes = Process.GetProcessesByName(processFileName, Environment.MachineName);

            foreach (Process process in processes)
            {

                var cmdLine = GetCommandLine(process);
                if (cmdLine.Contains("/queue:" + queueId)) return process;
            }
            return null;
        }

        private static string GetCommandLine(Process process)
        {
            //TODO: Need to find faster and better way to find process info
            try
            {
                var commandLine = new StringBuilder();

                using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                {
                    foreach (var @object in searcher.Get())
                    {
                        commandLine.Append(@object["CommandLine"] + " ");
                    }
                }

                return commandLine.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        void process_Exited(object sender, EventArgs e)
        {
            if (!_started) return;
            
            //process is exited, it should be restarted if it is a crash.
            Process process = (Process)sender;
            String handle = GetJobHandleFromProcess(process);
            if (String.IsNullOrEmpty(handle))
            {
                Logger.ErrorFormat("Process with unknown handle exited. Process Id {0} Machine Name {1}", process.Id, process.MachineName);
                return;
            }
            if (activeProcesses.ContainsKey(handle))
            {
                var processInfo = activeProcesses[handle];
                var retValue = process.ExitCode;
                if (retValue == -1)
                {
                    Logger.WarnFormat("Worker with ProcessId {0} and queue {1} stopped because queue is not supported.", process.Id, processInfo.QueueId);
                    activeProcesses.Remove(handle);
                    return;
                }

                //process is ended, restart the process, it will have new id.
                Logger.WarnFormat("Job terminated unexpectedly, job handle {0} for queue {1}. Restarting!!", handle, processInfo.QueueId);
                InnerStart(processInfo.QueueId, processInfo.DocStoreAddresses, handle);

            }
        }

        public bool Stop(string jobHandle)
        {
            if (!activeProcesses.ContainsKey(jobHandle)) return false;

            var info = activeProcesses[jobHandle];
            var process = info.Process;

            //SendText(process.MainWindowHandle, "x");
            //var exited = process.WaitForExit(5000);
            //if (exited == false) process.Kill();
            process.Exited -= process_Exited; //remove handler 
            if (process.HasExited) return true; //already closed.

            process.Kill();
            activeProcesses.Remove(jobHandle);
            return true;
        }

        public bool Restart(string jobHandle)
        {
            if (!activeProcesses.ContainsKey(jobHandle)) return false;

            var activeProcess = activeProcesses[jobHandle];
            if (!Stop(jobHandle)) 
            {
                Logger.ErrorFormat("Unable to stop job with handle {0}", jobHandle);
                return false;
            }
            //Restart the same job with the same handle.
            InnerStart(activeProcess.QueueId, activeProcess.DocStoreAddresses, jobHandle);
            return true;
        }

        public void Stop()
        {
            _started = false;
            foreach (var jobHandle in activeProcesses.Keys.ToList())
            {
                Stop(jobHandle);
            }
        }


    }
}

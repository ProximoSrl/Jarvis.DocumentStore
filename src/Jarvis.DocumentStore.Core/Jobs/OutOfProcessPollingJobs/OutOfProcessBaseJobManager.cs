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
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using System.Threading;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Collections.Concurrent;

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

            public Dictionary<String,String> CustomParameters { get; set; }

            public List<String> DocStoreAddresses { get; set; }
        }

        ConcurrentDictionary<String, ProcessInfo> activeProcesses;

        public ILogger Logger { get; set; }

        /// <summary>
        /// It is a rare situation, but it happened before that a process could not be 
        /// restarted for various reasons. This timer is used to verify that all processes
        /// are still alive.
        /// </summary>
        private readonly Timer _monitorTimer;

        public OutOfProcessBaseJobManager(DocumentStoreConfiguration configuration)
        {
            _configuration = configuration;
            activeProcesses = new ConcurrentDictionary<string, ProcessInfo>();
            Logger = NullLogger.Instance;
            //check each 10 minutes if some process is dead and was not started.
            _monitorTimer = new Timer(CheckProcesses, null, 20 * 1000, 10 * 60 * 1000);
        }

        public string Start(String queueName, Dictionary<String, String> customParameters, List<string> docStoreAddresses)
        {
            String processHandle = Guid.NewGuid().ToString() + "-" + Environment.MachineName;
            InnerStart(queueName, customParameters, docStoreAddresses, processHandle);
            _started = true;
            return processHandle;
        }

        private void InnerStart(
            string queueId, 
            Dictionary<String, String> customParameters, 
            List<string> docStoreAddresses, 
            String processHandle)
        {
            var jobsLauncherFileExe = @"JobsRunner\Jarvis.DocumentStore.Jobs.exe";
            if (customParameters.ContainsKey("location")) jobsLauncherFileExe = customParameters["location"];
            FileInfo fi = new FileInfo(jobsLauncherFileExe);
            Logger.InfoFormat("Launching external job process {0} for queue {1}", fi.FullName,  queueId);
            if (!fi.Exists)
                throw new ApplicationException("Unable to find executable for " + queueId + " queue in location " + fi.FullName);
            Process process = GetLocalProcessForQueue(queueId, jobsLauncherFileExe);
            if (process == null)
            {
                process = new System.Diagnostics.Process();
                process.StartInfo.FileName = fi.FullName;
                process.StartInfo.Arguments = "/dsuris:" + docStoreAddresses.Aggregate((s1, s2) => s1 + "|" + s2) +
                    " /queue:" + queueId +
                    " /handle:" + processHandle;
                process.StartInfo.WorkingDirectory = fi.Directory.FullName;
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
                Boolean started = process.Start();
                Logger.DebugFormat("Started process for queue {0} with ProcessId {1}.", queueId, process.Id);
            }
            else
            {
                Logger.DebugFormat("Reattached process for queue {0} with ProcessId {1}.", queueId, process.Id);
            }
            process.EnableRaisingEvents = true;
            process.Exited += process_Exited;

            var info = new ProcessInfo()
            {
                Process = process,
                QueueId = queueId,
                CustomParameters = customParameters,               
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
            //locking prevent that restarting a dead service is executed concurrently to periodic check.
            lock(this)
            {
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
                        Logger.WarnFormat("Worker with ProcessId {0} and queue {1} stopped because of internal error, the job cannot execute.", process.Id, processInfo.QueueId);
                        ProcessInfo pi;
                        activeProcesses.TryRemove(handle, out pi);
                        return;
                    }

                    //process is ended, restart the process, it will have new id.
                    Thread.Sleep(1000); //if for some reason the executable is stuck, not waiting for restart can kill the machine.
                    Logger.WarnFormat("Job terminated unexpectedly, job handle {0} for queue {1}. Restarting!!", handle, processInfo.QueueId);
                    InnerStart(processInfo.QueueId, processInfo.CustomParameters, processInfo.DocStoreAddresses, handle);
                }
            }
        }

        public bool Stop(string jobHandle)
        {
            if (!activeProcesses.ContainsKey(jobHandle)) return false;

            var info = activeProcesses[jobHandle];
            var process = info.Process;

            process.Exited -= process_Exited; //remove handler 
            if (process.HasExited) return true; //already closed.

            process.Kill();
            ProcessInfo pi;
            activeProcesses.TryRemove(jobHandle, out pi);
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
            InnerStart(activeProcess.QueueId, activeProcess.CustomParameters, activeProcess.DocStoreAddresses, jobHandle);
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

        private void CheckProcesses(object state)
        {
            lock (this)
            {
                foreach (var activeProcess in activeProcesses.ToList())
                {
                    try
                    {
                        if (activeProcess.Value.Process != null)
                        {
                            var activeProcessInfo = activeProcess.Value;
                            var processId = activeProcessInfo.Process.Id;
                            var process = Process.GetProcessById(processId);
                            if (process == null || process.HasExited)
                            {
                                Logger.ErrorFormat("Queue {0} job is not active anymore but it was not automatically restarted", activeProcess.Value.QueueId);
                                InnerStart(activeProcessInfo.QueueId, activeProcessInfo.CustomParameters, activeProcessInfo.DocStoreAddresses, activeProcess.Key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat(ex, "Error checking process key {0} queue {1}", activeProcess.Key, activeProcess.Value.QueueId);
                    }

                }
            }
        }

    }
}

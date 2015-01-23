using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{


    /// <summary>
    /// Suppose that the process is an external executable with some parameters.
    /// </summary>
    public class OutOfProcessBaseJobManager : IPollerJobManager
    {
        //[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool SetForegroundWindow(IntPtr hWnd);

        //public void SendText(IntPtr hwnd, string keys)
        //{
        //    if (hwnd != IntPtr.Zero)
        //    {
        //        if (SetForegroundWindow(hwnd))
        //        {
        //            System.Windows.Forms.SendKeys.SendWait(keys);
        //        }
        //    }
        //}

        Dictionary<String, Process> activeProcesses;

        public OutOfProcessBaseJobManager()
        {
            activeProcesses = new Dictionary<string, Process>();
        }

        public string Start(string queueId, List<string> docStoreAddresses)
        {
            var thisFileName = Environment.CommandLine.Split(' ')[0]
                .Trim('/', '"')
                .Replace(".vshost.exe", ".exe");

            System.Diagnostics.Process process =new System.Diagnostics.Process();
            process.StartInfo.FileName = thisFileName;
            process.StartInfo.Arguments = docStoreAddresses.Aggregate((s1, s2) => s1 + " " + s2) + " " + queueId;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = false;
            process.EnableRaisingEvents = true;
            process.Exited += process_Exited;

            Boolean started = process.Start();
            if (!started) return "";
            //process is started, 
         
            String processId = Environment.MachineName + ":" + process.Id;
            activeProcesses.Add(processId, process);
            return processId;
        }

        void process_Exited(object sender, EventArgs e)
        {

        }

        public bool Stop(string jobHandle)
        {
            var process = activeProcesses[jobHandle];
            if (process == null) return false;
            
            //SendText(process.MainWindowHandle, "x");
            //var exited = process.WaitForExit(5000);
            //if (exited == false) process.Kill();
            if (process.HasExited) return true; //already closed.

            process.Kill();
            activeProcesses.Remove(jobHandle);
            return true;
        }
    }
}

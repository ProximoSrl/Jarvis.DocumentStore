using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Castle.Core.Logging;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
#pragma warning disable S2486 // Generic exceptions should not be ignored
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
    public static class OfficeUtils
    {
        public static ILogger Logger { get; internal set; }

        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public static void SafeClose(this Microsoft.Office.Interop.Excel.Application app)
        {
            GetWindowThreadProcessId(new IntPtr(app.Hwnd), out var processId);
            app.Quit();
            KillProcess(processId);
        }

        public static void SafeClose(this Microsoft.Office.Interop.PowerPoint.Application app)
        {
            GetWindowThreadProcessId(new IntPtr(app.HWND), out var processId);
            app.Quit();
            KillProcess(processId);
        }

        public static void KillOfficeProcess(IntPtr processPointer)
        {
            try
            {
                Int32 processId;
                GetWindowThreadProcessId(processPointer, out processId);
                KillProcess(processId);
            }
            catch (Exception)
            {
                //Intentionally left empty
            }
        }

        private static void KillProcess(int processId)
        {
            var process = Process.GetProcessById(processId);
            try
            {
                Logger.InfoFormat("Killing Office process {0}", process.ProcessName);
                process.Kill();
            }
            catch (Exception)
            {
                //Intentionally left empty
            }
        }

        public static void KillOfficeProcess(String processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    var cmdLine = process.GetCommandLine();
                    if (cmdLine.IndexOf("automation", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            Logger.InfoFormat("Killing Office process {0}", process.ProcessName);
                            process.Kill();
                        }
                        catch (Exception)
                        {
                            //Intentionally left empty
                        }
                    }
                }
            }
            catch (Exception)
            {
                //Intentionally left empty
            }
        }

        /// <summary>
        /// Kills all office process started by automation more than one hour ago, they are 
        /// surely stale.
        /// </summary>
        public static void KillStaleOfficeProgram()
        {
            HashSet<String> processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "EXCEL",
                "WINWORD",
                "POWERPNT"
            };
            var now = DateTime.Now;
            foreach (var processName in processNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        var cmdLine = process.GetCommandLine();
                        if (cmdLine.IndexOf("automation", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (now.Subtract(process.StartTime).TotalMinutes > 60)
                            {
                                try
                                {
                                    Logger.InfoFormat("Killing Office process {0} because it is stale", process.ProcessName);
                                    process.Kill();
                                }
                                catch (Exception)
                                {
                                    //Intentionally left empty
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //Intentionally left empty
                }
            }
        }

        private static string GetCommandLine(this Process process)
        {
            var commandLine = new StringBuilder(process.MainModule.FileName);

            commandLine.Append(" ");
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            {
                foreach (var @object in searcher.Get())
                {
                    commandLine.Append(@object["CommandLine"]);
                    commandLine.Append(" ");
                }
            }

            return commandLine.ToString();
        }
#pragma warning restore S2486 // Generic exceptions should not be ignored
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
    }
}

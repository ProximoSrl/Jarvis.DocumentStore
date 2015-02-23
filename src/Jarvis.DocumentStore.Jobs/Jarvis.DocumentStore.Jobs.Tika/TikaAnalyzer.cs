using System;
using System.Diagnostics;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public interface ITikaAnalyzer
    {
        string GetHtmlContent(String pathToInputFile, String password);
    }

    public class TikaAnalyzer : ITikaAnalyzer
    {
        public TikaAnalyzer(JobsHostConfiguration jobsHostConfiguration)
        {
            JobsHostConfiguration = jobsHostConfiguration;
        }

        public ILogger Logger { get; set; }
        private JobsHostConfiguration JobsHostConfiguration { get; set; }

        public string GetHtmlContent(string pathToInputFile, String password)
        {
            string pathToJavaExe = JobsHostConfiguration.GetPathToJava();

            var arguments = String.Format(
                "-jar {0} -h \"{1}\"", 
                JobsHostConfiguration.GetPathToTika(), 
                pathToInputFile
            );

            Logger.DebugFormat("Executing {0} {1}", pathToJavaExe, arguments);

            var psi = new ProcessStartInfo(pathToJavaExe, arguments)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            string content = null;
            using (var p = Process.Start(psi))
            {
                //
                // Read in all the text from the process with the StreamReader.
                //
                using (var reader = p.StandardOutput)
                {
                    content = reader.ReadToEnd();
                }

                p.WaitForExit();
            }

            return content;
        }
    }
}

using System;
using System.Diagnostics;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using System.IO;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public interface ITikaAnalyzer
    {
        string GetHtmlContent(String pathToInputFile, String password);
        String Describe();
    }

    public class TikaAnalyzer : ITikaAnalyzer
    {
        List<String> tikaPaths;

        public TikaAnalyzer(JobsHostConfiguration jobsHostConfiguration)
        {
            JobsHostConfiguration = jobsHostConfiguration;

            var tikaHome = JobsHostConfiguration.GetConfigValue("TIKA_HOME");
            if (!File.Exists(tikaHome))
            {
                throw new Exception(string.Format("Tika not found on {0}", tikaHome));
            }

            var tikaLocation = Path.GetDirectoryName(tikaHome);
            var allTikaFiles = Directory.GetFiles(tikaLocation, "tika*.jar");
            tikaPaths = new List<string>();
            tikaPaths.Add(tikaHome);
            foreach (var tikaFile in allTikaFiles)
            {
                if (File.Exists(tikaFile) && !(tikaFile == tikaHome))
                {
                    tikaPaths.Add(tikaFile);
                }
            }
        }

        public ILogger Logger { get; set; }
        private JobsHostConfiguration JobsHostConfiguration { get; set; }

        private String currentPathToTika;

        public string GetHtmlContent(string pathToInputFile, String password)
        {
            string pathToJavaExe = JobsHostConfiguration.GetPathToJava();

            foreach (var tika in tikaPaths)
            {
                try
                {
                    var arguments = String.Format("-jar {0} -h \"{1}\"", tika, pathToInputFile);

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
                        //wait for 30 seconds with timeout, if extraction requires more than 30 seconds file is really too big or problematic
                        var closedCorrectly = p.WaitForExit(1000 * 30);
                        if (!closedCorrectly) 
                        {
                            //we have a timeout, ok we really need to kill the process and consider impossible to extract text from this file
                            p.Kill();
                            continue;
                        }

                        using (var reader = p.StandardOutput)
                        {
                            content = reader.ReadToEnd();
                        }


                        if (p.ExitCode == 0)
                        {
                            return content;
                        }
                        else
                        {
                            Logger.ErrorFormat("failed extracting with {0} exit code {1}", tika, p.ExitCode);
                        }
                    }
                }
                catch (Exception)
                {
                    Logger.ErrorFormat("Error extracting with tika {0}", tika);
                }
            }

            throw new ApplicationException("Unable to extract tika with all of present extractors");
        }

        public string Describe()
        {
            return "Tika out of process extractor: " + JobsHostConfiguration.GetConfigValue("TIKA_HOME");
        }
    }
}

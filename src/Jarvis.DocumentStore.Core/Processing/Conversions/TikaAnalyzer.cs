using System;
using System.Diagnostics;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Services;

namespace Jarvis.DocumentStore.Core.Processing.Conversions
{
    public interface ITikaAnalyzer
    {
        string GetHtmlContent(string pathToInputFile);
    }

    public class TikaAnalyzer : ITikaAnalyzer
    {
        public TikaAnalyzer(ConfigService configService)
        {
            ConfigService = configService;
        }

        public ILogger Logger { get; set; }
        private ConfigService ConfigService { get; set; }

        public string GetHtmlContent(string pathToInputFile)
        {
            string pathToJavaExe = ConfigService.GetPathToJava();

            var arguments = String.Format(
                "-jar {0} -h \"{1}\"", 
                ConfigService.GetPathToTika(), 
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

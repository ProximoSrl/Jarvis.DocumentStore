using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions
{
    public class TikaAnalyzer
    {
        public TikaAnalyzer(ConfigService configService)
        {
            ConfigService = configService;
        }

        public ILogger Logger { get; set; }
        private ConfigService ConfigService { get; set; }

        public void Run(string pathToInputFile, Action<string> writer)
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

            using (var p = Process.Start(psi))
            {
                //
                // Read in all the text from the process with the StreamReader.
                //
                using (var reader = p.StandardOutput)
                {
                    var content = reader.ReadToEnd();
                    writer(content);
                }

                p.WaitForExit();
            }
        }
    }
}


using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Attachments
{
    public class SevenZipExtractorFunctions
    {
        public ILogger Logger { get; set; }

        public SevenZipExtractorFunctions()
        {
            foreach (var item in tentativeDirectories)
            {
                if (File.Exists(item))
                {
                    ExecutablePath = item;
                }
            }

            if (String.IsNullOrEmpty(ExecutablePath))
            {
                ErrorStatus = "7z.exe not found in usual location: " + String.Join(",", tentativeDirectories);
            }

            Logger = NullLogger.Instance;
        }

        private List<String> tentativeDirectories = new List<string>()
        {
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe"
        };

        private String ExecutablePath;

        private String ErrorStatus;

        public Boolean IsOk { get { return String.IsNullOrEmpty(ErrorStatus); } }

        public String GetErrorStatus() { return ErrorStatus; }

        public IEnumerable<String> ExtractTo(String archiveFile, String destinationDirectory)
        {
            if (!String.IsNullOrEmpty(ErrorStatus))
            {
                Logger.Error(ErrorStatus);
                throw new ApplicationException(ErrorStatus);
            }

            //7z x archive.zip -oc:\soft
            var di = new DirectoryInfo(destinationDirectory);
            if (di.Exists)
            {
                di.Delete(true);
            }
            di.Create();
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = ExecutablePath;
                process.StartInfo.Arguments = String.Format("x \"{0}\" -o\"{1}\"", archiveFile, destinationDirectory);
                process.StartInfo.WorkingDirectory = destinationDirectory;
                process.StartInfo.UseShellExecute = false;

                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                String output;
                using (var reader = process.StandardOutput)
                {
                    output = reader.ReadToEnd();
                }
                Logger.DebugFormat("Output of 7z.exe is: " + output);
                process.WaitForExit();
            }
            foreach (var file in di.GetFiles("*.*", SearchOption.AllDirectories))
            {
                yield return file.FullName;
            }
  
        }
    }
}

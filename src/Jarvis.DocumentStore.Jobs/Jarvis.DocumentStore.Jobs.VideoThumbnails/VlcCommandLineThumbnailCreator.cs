using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.VideoThumbnails
{
    public class VlcCommandLineThumbnailCreator
    {
        private string vlcExecutable;
        private string format;
        private IExtendedLogger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vlcExecutable"></param>
        /// <param name="format"></param>
        /// <param name="logger"></param>
        public VlcCommandLineThumbnailCreator(
            string vlcExecutable, 
            string format, 
            IExtendedLogger logger)
        {
            this.vlcExecutable = vlcExecutable;
            this.format = format;
            this.logger = logger;
        }

        /// <summary>
        /// Creates the thumbnail and return fileName of newly created thumbnail.
        /// </summary>
        /// <param name="uriOfNetworkStream"></param>
        /// <param name="tempDir"></param>
        /// <param name="secondsOffset">Offset in seconds before taking the snapshot</param>
        /// <returns></returns>
        public String CreateThumbnail(String uriOfNetworkStream, String tempDir, Int32 secondsOffset)
        {
            var arguments = String.Format(
                   "{0} --rate=1 --video-filter=scene --vout=dummy --start-time={3} --stop-time={4} --scene-format={2} --scene-ratio=24 --scene-prefix=snap --scene-path={1} vlc://quit",
                   uriOfNetworkStream,
                   tempDir,
                   format,
                   secondsOffset,
                   secondsOffset + 10);
            
            logger.DebugFormat("Executing {0} {1}", vlcExecutable, arguments);

            var psi = new ProcessStartInfo(vlcExecutable, arguments)
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
                p.WaitForExit();
            }

            //Need to find thumbnails, VLC creates multiple files, but the one with the highest number
            //is the correct one.
            var file = Directory.GetFiles(tempDir, "snap*.*")
                .Select(f => new FileInfo(f))                
                .OrderByDescending(fg => fg.Length)
                .FirstOrDefault();

            if (file == null)
            {
                logger.ErrorFormat("Unable to find snapshot file in directory {0}. Files in directory are {1}", 
                    tempDir,
                    Directory.GetFiles(tempDir).Aggregate((s1, s2) => s1 + ", " + s2));
                return "";
            }
            return file.FullName;
        }
    }
}

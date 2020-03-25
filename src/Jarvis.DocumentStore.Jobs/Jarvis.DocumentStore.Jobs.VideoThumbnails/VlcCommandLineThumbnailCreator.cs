using Castle.Core.Logging;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
namespace Jarvis.DocumentStore.Jobs.VideoThumbnails
{
    public class VlcCommandLineThumbnailCreator
    {
        private readonly string vlcExecutable;
        private readonly string format;
        private readonly IExtendedLogger logger;

        /// <summary>
        /// Convert video to thumbnail with vlc player.
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
            String commandLineArgument = ConfigurationManager.AppSettings["vlc_commandline"];
            if (String.IsNullOrEmpty(commandLineArgument))
            {
                commandLineArgument = "{0} --rate=1 --video-filter=scene --start-time={3} --stop-time={4} --scene-format={2} --scene-ratio=24 --scene-prefix=snap --scene-path={1} vlc://quit";
            }

            var arguments = String.Format(
                  commandLineArgument,
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

            using (var p = Process.Start(psi))
            {
                //wait for 2 minutes.
                var exited = p.WaitForExit(1000 * 60 * 2);
                if (!exited)
                {
                    logger.WarnFormat("Vlc does not stopped after 2 minutes, killing");
                    p.Kill();
                }
            }

            //Need to find thumbnails, VLC creates multiple files, but the one with the highest number
            //is the correct one.
            var file = Directory.GetFiles(tempDir, "snap*.*")
                .Select(f => new FileInfo(f))
                .OrderByDescending(fg => fg.Length)
                .FirstOrDefault();

            if (file == null)
            {
                String fileList = "";
                var files = Directory.GetFiles(tempDir).ToList();
                if (files.Count > 0)
                {
                    fileList = files.Aggregate((s1, s2) => s1 + ", " + s2);
                }
                logger.ErrorFormat("Unable to find snapshot file in directory {0}. Files actually in directory: {1}", tempDir, fileList);
                return "";
            }
            return file.FullName;
        }
    }
}

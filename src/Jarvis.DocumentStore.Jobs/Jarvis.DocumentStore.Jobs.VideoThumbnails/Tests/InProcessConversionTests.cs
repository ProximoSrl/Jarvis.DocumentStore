using Castle.Core.Logging;
using Jarvis.DocumentStore.Jobs.VideoThumbnails;
using Jarvis.DocumentStore.Jobs.VideoThumbnails.Tests;
using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Jobs.Tika.Tests
{
    public class BaseConversionTests : IPollerTest
    {
        public string Name
        {
            get
            {
                return "Video thumbnail extraction";
            }
        }

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();
            const String format = "png";
            var vlcExecutable = Helper.GetExecutableLocation();

            if (vlcExecutable == null)
            {
                retValue.Add(new PollerTestResult(false, "Executable location, use app settings vlc_location"));
                return retValue;
            }
            else
            {
                retValue.Add(new PollerTestResult(true, "Executable location, "));
            }

            try
            {
                var worker = new VlcCommandLineThumbnailCreator(vlcExecutable, format, NullLogger.Instance);

                var tempFile = Path.Combine(Path.GetTempPath(), "video.mp4");
                if (File.Exists(tempFile)) File.Delete(tempFile);
                File.WriteAllBytes(tempFile, TestFiles.video);

                var thumb = worker.CreateThumbnail(tempFile, Path.GetTempPath(), 4);
                retValue.Add(new PollerTestResult(!String.IsNullOrEmpty(thumb), "video thumb extraction: "));
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, "video thumb extraction: " + ex.ToString()));
            }

            return retValue;
        }
    }
}

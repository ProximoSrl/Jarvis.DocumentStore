using System.Diagnostics;
using System.IO;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class ConvertEmailTests
    {
        [Test]
        public void convert()
        {
            var task = new MailMessageToHtmlConverterTask()
            {
                Logger = new ConsoleLogger()
            };

            var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(TestConfig.PathToEml));
            if(File.Exists(tmpFile))
                File.Delete(tmpFile);

            File.Copy(TestConfig.PathToEml, tmpFile);

            var file = task.Convert(tmpFile, Path.GetTempPath());
            Debug.WriteLine("Saved to {0}", (object)file);
            
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            Assert.Inconclusive();
        }
    }
}

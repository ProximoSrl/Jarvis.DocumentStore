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

            var file = task.Convert(TestConfig.PathToEml, Path.GetTempPath());
            Debug.WriteLine("Saved to {0}", (object)file);
            
            Assert.Inconclusive();
        }
    }
}

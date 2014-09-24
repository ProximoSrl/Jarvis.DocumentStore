using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.PipelineTests
{
    [TestFixture]
    public class ConvertEmailTests
    {
        [Test]
        public void convert()
        {
            var task = new MailMessageToHtmlConverterTask();
            var file = task.Convert(TestConfig.PathToEml, Path.GetTempPath());
            Debug.WriteLine("Saved to {0}", (object)file);
            Assert.Inconclusive();
        }
    }
}

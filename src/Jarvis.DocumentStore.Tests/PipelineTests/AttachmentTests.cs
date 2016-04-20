using Jarvis.DocumentStore.Jobs.Attachments;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Jarvis.Framework.TestHelpers;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class AttachmentTests
    {
        SevenZipExtractorFunctions sut;

        [SetUp]
        public void SetUp()
        {
            sut = new SevenZipExtractorFunctions();
            sut.Logger = new TestLogger() { Level = Castle.Core.Logging.LoggerLevel.Debug};
        }


        [Test]
        public void verify_7zip_extraction()
        {
            var tempDirectory = Path.GetTempPath();
            tempDirectory = Path.Combine(tempDirectory, "attachmentsTest");
            var files = sut.ExtractTo(TestConfig.PathTo7Zip, tempDirectory).ToList();
            Assert.That(files, Has.Count.EqualTo(3));
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }
        }

        [Test]
        public void verify_rar_extraction()
        {
            var tempDirectory = Path.GetTempPath();
            tempDirectory = Path.Combine(tempDirectory, "attachmentsTest");
            var files = sut.ExtractTo(TestConfig.PathToRar, tempDirectory).ToList();
            Assert.That(files, Has.Count.EqualTo(3));
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }
        }
    }
}

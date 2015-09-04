using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;
using Jarvis.DocumentStore.Jobs.LibreOffice;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Jarvis.DocumentStore.Jobs.HtmlZipOld;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture(Category = "integration")]
    public class TuesPeckinConversionTest
    {
        HtmlToPdfConverterFromDiskFileOld _converter;
        JobsHostConfiguration _config;
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _config = new JobsHostConfiguration();
        }

        [Test]
        public void verify_preview_of_single_html_file()
        {
            _converter = new HtmlToPdfConverterFromDiskFileOld(TestConfig.PathToSimpleHtmlFile, _config);
            _converter.Logger = NullLogger.Instance;
            var result = _converter.Run("testTenant", "jobtest");

            Assert.That(File.Exists(result), "Output pdf file not created");
            File.Delete(result);
        }
    }
}

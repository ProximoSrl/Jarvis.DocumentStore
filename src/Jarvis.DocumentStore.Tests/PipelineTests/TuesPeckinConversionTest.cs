using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using NUnit.Framework;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Jarvis.DocumentStore.Jobs.HtmlZipOld;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture(Category = "integration")]
    public class TuesPeckinConversionTest
    {
        HtmlToPdfConverterFromDiskFileOld _converter;
        SafeHtmlConverter _sanitizer;
        JobsHostConfiguration _config;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = new JobsHostConfiguration();
        }

        [Test]
        public void Verify_preview_of_single_html_file()
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".html");
            File.Copy(TestConfig.PathToSimpleHtmlFile, tempFile);
            _converter = new HtmlToPdfConverterFromDiskFileOld(tempFile, _config);
            _converter.Logger = NullLogger.Instance;
            var result = _converter.Run("jobtest");

            Assert.That(File.Exists(result), "Output pdf file not created");
            File.Delete(result);
        }


        [Test]
        public void Verify_sanitize_of_single_html_file()
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".html");
            File.Copy(TestConfig.PathToSimpleHtmlFile, tempFile);
            _sanitizer = new SafeHtmlConverter(tempFile)
            {
                Logger = NullLogger.Instance
            };
            var result = _sanitizer.Run("jobtest");

            Assert.That(File.Exists(result), "HTML file sanitized");
            File.Delete(result);
        }


        [Test]
        public void Verify_sanitize_of_single_html_file_with_base64_image()
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".html");
            File.Copy(TestConfig.PathToHtmlFileWithBase64Image, tempFile);
            _sanitizer = new SafeHtmlConverter(tempFile)
            {
                Logger = NullLogger.Instance
            };
            var result = _sanitizer.Run("jobtest");

            Assert.That(File.Exists(result), "HTML file sanitized");
            string html = File.ReadAllText(result);
            Assert.IsTrue(html.Contains("<img src=\"data:image/png;base64"), "base64 images are allowed");
            File.Delete(result);
        }

        [Test]
        public void Verify_sanitize_of_single_mhtml_file()
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".mht");
            File.Copy(TestConfig.PathToMht, tempFile);

            string mhtml = File.ReadAllText(tempFile);
            MHTMLParser parser = new MHTMLParser(mhtml)
            {
                OutputDirectory = Path.GetDirectoryName(tempFile),
                DecodeImageData = true
            };
            var outFile = Path.ChangeExtension(tempFile, ".html");
            File.WriteAllText(outFile, parser.getHTMLText());

            _sanitizer = new SafeHtmlConverter(outFile)
            {
                Logger = NullLogger.Instance
            };
            var result = _sanitizer.Run("jobtest");

            Assert.That(File.Exists(result), "Output pdf file not created");
            File.Delete(result);
        }
    }
}

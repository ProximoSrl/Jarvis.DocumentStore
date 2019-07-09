using Jarvis.DocumentStore.Jobs.PdfThumbnails;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class ThumbnailFromPdfExtractionTest
    {
        [Test]
        public async Task Generate_with_password_removal()
        {
            var file = TestConfig.PathToPasswordProtectedPdf;
            CreateImageFromPdfTask sut = new CreateImageFromPdfTask();
            sut.Decryptor = new PdfDecrypt() { Logger = new TestLogger() };
            sut.Passwords.Add("jarvistest");

            var convertParams = new CreatePdfImageTaskParams()
            {
                Dpi = 150,
                FromPage = 1,
                Pages = 1,
                Format = CreatePdfImageTaskParams.ImageFormat.Jpg,
            };
            Boolean wasCalled = false;
            await sut.Run(
                file,
                convertParams,
                (i, s) =>
                {
                    wasCalled = true;
                    return Task.FromResult<Boolean>(true);
                }
            ).ConfigureAwait(false);

            Assert.IsTrue(wasCalled, "conversion failed.");
        }

        [Test]
        public async Task Generate_smoke()
        {
            var file = TestConfig.PathToDocumentPdf;
            CreateImageFromPdfTask sut = new CreateImageFromPdfTask();

            var convertParams = new CreatePdfImageTaskParams()
            {
                Dpi = 600,
                FromPage = 1,
                Pages = 1,
                Format = CreatePdfImageTaskParams.ImageFormat.Png,
            };
            Boolean wasCalled = false;
            await sut.Run(
                file,
                convertParams,
                (i, s) =>
                {
                    wasCalled = true;

                    var tempFile = Path.GetTempFileName() + ".jpg";
                    using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        s.CopyTo(fs);
                    }
                    System.Diagnostics.Process.Start(tempFile);
                    return Task.FromResult<Boolean>(true);
                }
            ).ConfigureAwait(false);

            Assert.IsTrue(wasCalled, "conversion failed.");
        }
    }
}

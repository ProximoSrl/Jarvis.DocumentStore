using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Jobs.PdfThumbnails;
using Jarvis.DocumentStore.Jobs.Tika;
using Jarvis.Framework.TestHelpers;
using java.io;
using java.net;
using NUnit.Framework;
using org.apache.tika;
using org.apache.tika.metadata;
using org.apache.tika.parser;
using org.apache.tika.sax;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class ThumbnailFromPdfExtractionTest
    {
        [Test]
        public async void Generate_with_password_removal()
        {
            var file = TestConfig.PathToPasswordProtectedPdf;
            CreateImageFromPdfTask sut = new CreateImageFromPdfTask();
            sut.Decryptor = new PdfDecrypt() {Logger = new TestLogger()};
            sut.Passwords.Add("jarvistest");

            var convertParams = new CreatePdfImageTaskParams()
            {
                Dpi = 150,
                FromPage = 1,
                Pages = 1,
                Format = CreatePdfImageTaskParams.ImageFormat.Jpg,
            };
            Boolean wasCalled = false;
            var result = await sut.Run(
                file,
                convertParams,
                (i, s) =>
                {
                    wasCalled = true;
                    return Task.FromResult<Boolean>(true);
                }
            );

            Assert.IsTrue(wasCalled, "conversion failed.");

        }

        [Test]
        public async void Generate_smoke()
        {
            var file = TestConfig.PathToDocumentPdf;
            CreateImageFromPdfTask sut = new CreateImageFromPdfTask();

            var convertParams = new CreatePdfImageTaskParams()
            {
                Dpi = 150,
                FromPage = 1,
                Pages = 1,
                Format = CreatePdfImageTaskParams.ImageFormat.Jpg,
            };
            Boolean wasCalled = false;
            var result = await sut.Run(
                file,
                convertParams,
                (i, s) =>
                {
                    wasCalled = true;
                    return Task.FromResult<Boolean>(true);
                }
            );

            Assert.IsTrue(wasCalled, "conversion failed.");

        }
    }
}

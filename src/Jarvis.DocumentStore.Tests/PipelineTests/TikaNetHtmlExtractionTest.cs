using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Jobs.Tika;
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
    public class TikaNetHtmlExtractionTest
    {
        [Test]
        public void Extract_with_password_removal()
        {
            var file = TestConfig.PathToPasswordProtectedPdf;
            TikaNetAnalyzer sut = new TikaNetAnalyzer();
            var result = sut.GetHtmlContent(file, "jarvistest");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Extract_with_unprotected_file_passing_password()
        {
            var file = TestConfig.PathToDocumentPdf;
            TikaNetAnalyzer sut = new TikaNetAnalyzer();
            var result = sut.GetHtmlContent(file, "jarvistest");
            Assert.That(result, Is.Not.Null);
        }
    }
}

using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.DocumentStoreCore.Support
{
    [TestFixture]
    public class StandardDocumentFormatTranslatorTests
    {

        StandardDocumentFormatTranslator sut;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            sut = new StandardDocumentFormatTranslator();
        }

        [TestCase("test.pdf", "pdf")]
        [TestCase("test.pDF", "pdf")]
        [TestCase("test.unknown", null)]
        [TestCase("test.png", "rasterimage")]
        [TestCase("test.PNg", "rasterimage")]
        public void Name(String fileName, String expected)
        {
            var knownType = sut.GetFormatFromFileName(fileName);
            if (String.IsNullOrEmpty(expected))
                Assert.That(knownType, Is.Null);
            else
                Assert.That(knownType, Is.EqualTo(new Core.Domain.DocumentDescriptor.DocumentFormat(expected)));
        }
    }
}

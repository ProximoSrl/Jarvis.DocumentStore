using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [TestFixture]
    public class BlobIdTests
    {
        [Test]
        public void blobId_should_contain_format_and_number()
        {
            var id = new BlobId(new DocumentFormat("format"), 1);
            Assert.AreEqual("format.1", (string)id);
        }

        [Test]
        public void blobId_should_parse_format_from_string()
        {
            var id = new BlobId("format.100");
            Assert.AreEqual(new DocumentFormat("format"), id.Format);
        }
    }
}

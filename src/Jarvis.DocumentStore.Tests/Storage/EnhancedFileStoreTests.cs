using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;

namespace Jarvis.DocumentStore.Tests.ServicesTests
{
    [TestFixture]
    public class EnhancedFileStoreTests
    {
        private IBlobStore _originals;
        private IBlobStore _artifacts;
        private BlobStoreByFormat _manager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _originals = NSubstitute.Substitute.For<IBlobStore>();
            _artifacts = NSubstitute.Substitute.For<IBlobStore>();
            _manager = new BlobStoreByFormat(_originals, _artifacts);
        }

        [Test]
        public void Should_select_original()
        {
            var selected = _manager.ForFormat(DocumentFormats.Original);
            Assert.AreEqual(_originals, selected);
        }

        [Test]
        public void Should_select_artifacts()
        {
            var selected = _manager.ForFormat(DocumentFormats.Pdf);
            Assert.AreEqual(_artifacts, selected);
        }

        [Test]
        public void Should_select_original_from_blobId()
        {
            var selected = _manager.ForBlobId(new BlobId("original.1"));
            Assert.AreEqual(_originals, selected);
        }

        [Test]
        public void Should_select_artifacts_from_blobId()
        {
            var selected = _manager.ForBlobId(new BlobId("pdf.1"));
            Assert.AreEqual(_artifacts, selected);
        }

        [Test]
        public void Should_write_original_format_to_original_store()
        {
            var blobId = new BlobId(DocumentFormats.Original, 1);
            _originals.Upload(Arg.Any<DocumentFormat>(), Arg.Any<string>()).Returns(blobId);

            var id = _manager.Upload(DocumentFormats.Original, TestConfig.PathToDocumentPdf);

            Assert.AreEqual(blobId, id);
        }

        [Test]
        public void Should_write_pdf_format_to_original_store()
        {
            var blobId = new BlobId(DocumentFormats.Pdf, 1);
            _artifacts.Upload(Arg.Any<DocumentFormat>(), Arg.Any<string>()).Returns(blobId);

            var id = _manager.Upload(DocumentFormats.Pdf, TestConfig.PathToDocumentPdf);

            Assert.AreEqual(blobId, id);
        }

        [Test]
        public void Should_read_original_file_from_originals_store()
        {
            var blobId = new BlobId(DocumentFormats.Original, 1);
            _originals.Download(blobId, "path/to/nothing").Returns("a.file");          
            var fname = _manager.Download(blobId, "path/to/nothing");

            Assert.AreEqual("a.file", fname);
        }

        [Test]
        public void Should_read_pdf_file_from_artifacts_store()
        {
            var blobId = new BlobId(DocumentFormats.Pdf, 1);
            _artifacts.Download(blobId, "path/to/nothing").Returns("a.file");

            var fname = _manager.Download(blobId, "path/to/nothing");

            Assert.AreEqual("a.file", fname);
        }
    }
}
using System.Net;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    [TestFixture]
    public class DownloadTests : AbstractFileControllerTests
    {
        [Test]
        public void request_for_invalid_file_handle_should_404()
        {
            var documentHandle = new DocumentHandle("not_in_store");
            var format = new DocumentFormat("any_format");

            var response = Controller.GetFormat(_tenantId, documentHandle, format).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("Document not_in_store not found", response.GetError().Message);
        }

        [Test]
        public void request_for_missing_document_should_404()
        {
            // arrange
            var documentHandle = new DocumentHandle("doc");
            var info = new DocumentHandleInfo(
                new DocumentHandle("doc"),
                new FileNameWithExtension("a.file")
                );
            var format = new DocumentFormat("any_format");
            SetupDocumentHandle(info, new DocumentId(1));

            // act
            var response = Controller.GetFormat(_tenantId, documentHandle, format).Result;

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("Document doc not found", response.GetError().Message);
        }

        [Test]
        public void request_for_missing_format_should_404()
        {
            // arrange
            var info = new DocumentHandleInfo(
                new DocumentHandle("doc"),
                new FileNameWithExtension("a.file")
                );
            
            var format = new DocumentFormat("missing");

            var doc = new DocumentReadModel(
                new DocumentId(1),
                new BlobId("file_1")
                );

            SetupDocumentHandle(info, doc.Id);
            SetupDocumentModel(doc);

            // act
            var response = Controller.GetFormat(_tenantId, info.Handle, format).Result;

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("Document doc doesn't have format missing", response.GetError().Message);
        }

        [Test]
        public void when_file_is_not_found_should_return_404()
        {
            // arrange
            var info = new DocumentHandleInfo(
                new DocumentHandle("doc"),
                new FileNameWithExtension("a.file")
                );
            var format = new DocumentFormat("original");

            var blobId = new BlobId("file_1");
            var doc = new DocumentReadModel(
                new DocumentId(1),
                blobId
                );
            doc.AddHandle(info);

            SetupDocumentHandle(info, doc.Id);
            SetupDocumentModel(doc);

            BlobStore.GetDescriptor(blobId).Returns(i => null);

            // act
            var response = Controller.GetFormat(_tenantId, info.Handle, format).Result;

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("File file_1 not found", response.GetError().Message);
        }


        [Test]
        public void should_download_original_file()
        {
            // arrange
            var info = new DocumentHandleInfo(
                new DocumentHandle("doc"),
                new FileNameWithExtension("\"A document.docx\"")
                );

            var format = new DocumentFormat("original");

            var blobId = new BlobId("file_1");
            var doc = new DocumentReadModel(
                new DocumentId(1),
                blobId
                );
            doc.AddHandle(info);

            SetupDocumentHandle(info, doc.Id);
            SetupDocumentModel(doc);

            BlobStore
                .GetDescriptor(blobId)
                .Returns(i => new FsFileStoreDescriptor(blobId, TestConfig.PathToWordDocument));

            // act
            using (var response = Controller.GetFormat(_tenantId, info.Handle, format).Result)
            {
                // assert
                response.EnsureSuccessStatusCode();
                Assert.AreEqual("\"A document.docx\"", response.Content.Headers.ContentDisposition.FileName);
            }
        }

        [Test]
        public void should_download_pdf_format()
        {
            // arrange
            var info = new DocumentHandleInfo(
                new DocumentHandle("doc"),
                new FileNameWithExtension("a.file")
                );
            var format = new DocumentFormat("pdf");
            var pdfBlobId = new BlobId("pdf");

            var doc = new DocumentReadModel(
                new DocumentId(1),
                new BlobId("file_1")
                );

            doc.AddFormat(new PipelineId("abc"), format, pdfBlobId);

            SetupDocumentHandle(info, doc.Id);
            SetupDocumentModel(doc);

            BlobStore.GetDescriptor(pdfBlobId).Returns(i => new FsFileStoreDescriptor(pdfBlobId, TestConfig.PathToDocumentPdf));

            // act
            using (var response = Controller.GetFormat(_tenantId, info.Handle, format).Result)
            {
                // assert
                response.EnsureSuccessStatusCode();
                Assert.AreEqual("application/pdf", response.Content.Headers.ContentType.MediaType);
            }
        }
    }
}
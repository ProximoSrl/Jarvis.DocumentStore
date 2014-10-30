using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Castle.Core.Logging;
using CQRS.Kernel.Commands;
using CQRS.Kernel.Store;
using CQRS.Shared.Commands;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Controllers;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    public abstract class AbstractFileControllerTests
    {
        protected DocumentsController Controller;
        protected IFileStore FileStore;
        protected IIdentityGenerator IdentityGenerator;
        protected IReader<HandleToDocument, DocumentHandle> HandleToDocumentReader;
        protected IReader<DocumentReadModel, DocumentId> DocumentReader;
        protected TenantId _tenantId = new TenantId("docs");

        [SetUp]
        public void SetUp()
        {
            FileStore = Substitute.For<IFileStore>();
            IdentityGenerator = Substitute.For<IIdentityGenerator>();
            HandleToDocumentReader = Substitute.For<IReader<HandleToDocument, DocumentHandle>>();
            DocumentReader = Substitute.For<IReader<DocumentReadModel, DocumentId>>();
            var bus = Substitute.For<IInProcessCommandBus>();

            Controller = new DocumentsController(FileStore, new ConfigService(), IdentityGenerator, HandleToDocumentReader, DocumentReader, bus)
            {
                Request = new HttpRequestMessage
                {
                    RequestUri = new Uri("http://localhost/api/products")
                },
                Logger = new ConsoleLogger(),
                Configuration = new HttpConfiguration()
            };

          //  Controller.Configuration.MapHttpAttributeRoutes();

            Controller.RequestContext.RouteData = new HttpRouteData(
                route: new HttpRoute(),
                values: new HttpRouteValueDictionary { { "controller", "file" } });
            
            Controller.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
        }

        protected void SetupDocumentModel(DocumentReadModel doc)
        {
            this.DocumentReader.FindOneById(doc.Id).Returns(info => doc);
        }

        protected void SetupDocumentHandle(DocumentHandle documentHandle, DocumentId documentId)
        {
            HandleToDocumentReader.FindOneById(documentHandle).Returns(info => new HandleToDocument()
            {
                DocumentId = documentId
            });
        }
    }

    [TestFixture]
    public class UploadTests : AbstractFileControllerTests
    {
        [Test]
        public async void calling_upload_without_file_attachment_should_return_BadRequest()
        {
            var response = await Controller.Upload(_tenantId, new DocumentHandle("Document_1"));
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Test]
        public async void calling_upload_with_empty_attachment_should_return_BadRequest()
        {
            Controller.Request.Content = new MultipartFormDataContent("test");
            var response = await Controller.Upload(_tenantId, new DocumentHandle("Document_1"));
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("Attachment not found!", response.GetError().Message);
        }

        [Test]
        public async void calling_upload_with_unsupported_file_type_should_return_BadRequest()
        {
            var response = await upload_file(TestConfig.PathToInvalidFile);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("Unsupported file file.invalid", response.GetError().Message);
        }

        [Test]
        public async void calling_upload_with_supported_file_type_should_return_Ok()
        {
            IdentityGenerator.New<DocumentId>().Returns(new DocumentId(1));
            var descriptor = Substitute.For<IFileStoreDescriptor>();
            descriptor.Hash.Returns(new FileHash("abc"));
            FileStore.GetDescriptor(Arg.Any<FileId>()).Returns(descriptor);
            long streamLen = 0;
            HttpResponseMessage response = null;
            using (var stream = new MemoryStream())
            {
                FileStore.CreateNew(Arg.Any<FileId>(), Arg.Any<FileNameWithExtension>()).Returns(stream);
                response = await upload_file(TestConfig.PathToDocumentPdf);
                streamLen = stream.Length;
            }

            response.EnsureSuccessStatusCode();
            Assert.AreEqual(new FileInfo(TestConfig.PathToDocumentPdf).Length, streamLen);
        }

        private async Task<HttpResponseMessage> upload_file(string pathToFile)
        {
            using (var stream = new FileStream(pathToFile, FileMode.Open))
            {
                var multipartFormDataContent = new MultipartFormDataContent("test"){
                    {
                        new StreamContent(stream), 
                        Path.GetFileNameWithoutExtension(pathToFile),
                        Path.GetFileName(pathToFile)
                    }
                };

                Controller.Request.Content = multipartFormDataContent;

                return await Controller.Upload(_tenantId, new DocumentHandle("Document_1"));
            }
        }
    }

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
            var format = new DocumentFormat("any_format");
            SetupDocumentHandle(documentHandle, new DocumentId(1));

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
            var documentHandle = new DocumentHandle("doc");
            var format = new DocumentFormat("missing");

            var doc = new DocumentReadModel(
                new DocumentId(1),
                new FileId("file_1"),
                documentHandle,
                new FileNameWithExtension("document.docx")
            );

            SetupDocumentHandle(documentHandle, doc.Id);
            SetupDocumentModel(doc);

            // act
            var response = Controller.GetFormat(_tenantId, documentHandle, format).Result;

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("Document doc doesn't have format missing", response.GetError().Message);
        }

        [Test]
        public void when_file_is_not_found_should_return_404()
        {
            // arrange
            var documentHandle = new DocumentHandle("doc");
            var format = new DocumentFormat("original");

            var fileId = new FileId("file_1");
            var doc = new DocumentReadModel(
                new DocumentId(1),
                fileId,
                documentHandle,
                new FileNameWithExtension("A document.docx")
            );

            SetupDocumentHandle(documentHandle, doc.Id);
            SetupDocumentModel(doc);

            FileStore.GetDescriptor(fileId).Returns(i => null);

            // act
            var response = Controller.GetFormat(_tenantId, documentHandle, format).Result;

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("File file_1 not found", response.GetError().Message);
        }


        [Test]
        public void should_download_original_file()
        {
            // arrange
            var documentHandle = new DocumentHandle("doc");
            var format = new DocumentFormat("original");

            var fileId = new FileId("file_1");
            var doc = new DocumentReadModel(
                new DocumentId(1),
                fileId,
                documentHandle,
                new FileNameWithExtension("A document.docx")
            );


            SetupDocumentHandle(documentHandle, doc.Id);
            SetupDocumentModel(doc);

            FileStore.GetDescriptor(fileId).Returns(i => new FsFileStoreDescriptor(fileId, TestConfig.PathToWordDocument));

            // act
            using (var response = Controller.GetFormat(_tenantId, documentHandle, format).Result)
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
            var documentHandle = new DocumentHandle("doc");
            var format = new DocumentFormat("pdf");
            var pdfFileId = new FileId("pdf");

            var doc = new DocumentReadModel(
                new DocumentId(1),
                new FileId("file_1"),
                documentHandle,
                new FileNameWithExtension("A document.docx")
            );

            doc.AddFormat(new PipelineId("abc"), format, pdfFileId);

            SetupDocumentHandle(documentHandle, doc.Id);
            SetupDocumentModel(doc);

            FileStore.GetDescriptor(pdfFileId).Returns(i => new FsFileStoreDescriptor(pdfFileId, TestConfig.PathToDocumentPdf));

            // act
            using (var response = Controller.GetFormat(_tenantId, documentHandle, format).Result)
            {
                // assert
                response.EnsureSuccessStatusCode();
                Assert.AreEqual("application/pdf", response.Content.Headers.ContentType.MediaType);
            }
        }
    }
}

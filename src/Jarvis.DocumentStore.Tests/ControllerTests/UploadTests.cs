using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;
using System;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
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
        public async void calling_upload_with_handle_containing_at_char_return_bad_request()
        {
            var response = await InnerUploadFile("Document@otherhandle");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.Response.StatusCode);
        }

        [Test]
        public async void calling_upload_with_unsupported_file_type_should_return_BadRequest()
        {
            var response = await upload_file(TestConfig.PathToInvalidFile, "Document_1");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("Unsupported file file.invalid", response.GetError().Message);
        }

        [Test]
        public async void calling_upload_with_supported_file_type_should_return_Ok()
        {
            var response = await InnerUploadFile("Document_1");
            Assert.AreEqual(new FileInfo(TestConfig.PathToDocumentPdf).Length, response.StreamLength);       
            response.Response.EnsureSuccessStatusCode();
        }

        private async Task<InnerUploadFileResponse> InnerUploadFile(String documentHandle)
        {
            IdentityGenerator.New<DocumentId>().Returns(new DocumentId(1));
            var descriptor = Substitute.For<IBlobDescriptor>();
            descriptor.Hash.Returns(new FileHash("abc"));
            BlobStore.GetDescriptor(Arg.Any<BlobId>()).Returns(descriptor);
            long streamLen = 0;
            HttpResponseMessage response = null;
            using (var stream = new MemoryStream())
            {
                var fileWriter = new BlobWriter(new BlobId("sample_1"), stream, new FileNameWithExtension("a.file"));
                BlobStore.CreateNew(Arg.Any<DocumentFormat>(), Arg.Any<FileNameWithExtension>()).Returns(fileWriter);
                response = await upload_file(TestConfig.PathToDocumentPdf, documentHandle);
                streamLen = stream.Length;
            }


            return new InnerUploadFileResponse() {Response = response, StreamLength = streamLen};
        }

        public class InnerUploadFileResponse 
        {
            public HttpResponseMessage Response { get; set; }

            public Int64 StreamLength { get; set; }
        }

        private async Task<HttpResponseMessage> upload_file(string pathToFile, string documentHandle)
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

                return await Controller.Upload(_tenantId, new DocumentHandle(documentHandle));
            }
        }
    }
}

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Controllers;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NSubstitute;
using NUnit.Framework;
using FileInfo = System.IO.FileInfo;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    [TestFixture]
    public class FileUploadControllerTests
    {
        FileUploadController _controller;
        private IFileStore _fileStore;

        [SetUp]
        public void SetUp()
        {
            _fileStore = Substitute.For<IFileStore>();
            var cmdBus = Substitute.For<ICommandBus>();
            var im = Substitute.For<IIdentityGenerator>();

            _controller = new FileUploadController(_fileStore, new ConfigService(),cmdBus, im)
            {
                Request = new HttpRequestMessage(),
                Logger = new ConsoleLogger()
            };

            _controller.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
        }

        [Test]
        public async void calling_upload_without_file_attachment_should_return_BadRequest()
        {
            var response = await _controller.Upload(new FileAlias("Document_1"));
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Test]
        public async void calling_upload_with_empty_attachment_should_return_BadRequest()
        {
            _controller.Request.Content = new MultipartFormDataContent("test");
            var response = await _controller.Upload(new FileAlias("Document_1"));
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
            long streamLen = 0;
            HttpResponseMessage response = null;
            using (var stream = new MemoryStream())
            {
                _fileStore.CreateNew(Arg.Any<FileId>(), Arg.Any<string>()).Returns(stream);
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

                _controller.Request.Content = multipartFormDataContent;

                return await _controller.Upload(new FileAlias("Document_1"));
            }        
        }
    }
}

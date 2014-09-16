using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Controllers;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Jarvis.ImageService.Core.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.ControllerTests
{
    [TestFixture]
    public class ThumbnailControllerTests
    {
        ThumbnailController _controller;
        [SetUp]
        public void SetUp()
        {
            var store = Substitute.For<IFileStore>();
            var fileInfoService = Substitute.For<IFileInfoService>();
            _controller = new ThumbnailController(store, fileInfoService)
            {
                Request = new HttpRequestMessage()
            };
        }

        [Test]
        public async void calling_upload_without_file_attachment_should_return_BadRequest()
        {
            var response = await _controller.Upload("Document_1");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Test]
        public async void calling_upload_with_empty_attachment_should_return_BadRequest()
        {
            _controller.Request.Content = new MultipartFormDataContent("test");
            var response = await _controller.Upload("Document_1");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("Attachment not found!", response.GetError().Message);
        }

        [Test]
        public async void calling_upload_with_unsupported_file_type_should_return_BadRequest()
        {
            using (var stream = new FileStream(SampleData.PathToATextDocument, FileMode.Open))
            {
                var multipartFormDataContent = new MultipartFormDataContent("test"){
                    {
                        new StreamContent(stream), 
                        Path.GetFileNameWithoutExtension(SampleData.PathToATextDocument),
                        Path.GetFileName(SampleData.PathToATextDocument)
                    }
                };

                _controller.Request.Content = multipartFormDataContent;

                var response = await _controller.Upload("Document_1");

                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.AreEqual("Unsupported file .txt", response.GetError().Message);
            }
        }
    }
}

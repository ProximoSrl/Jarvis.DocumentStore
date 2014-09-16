using System;
using System.Collections.Generic;
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
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.ControllerTests
{
    [TestFixture]
    public class ThumbnailControllerTests
    {
        ThumbnailController _controller;
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
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
    }
}

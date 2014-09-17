using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Controllers
{
    public class FileUploadController : ApiController
    {
        readonly IImageService _imageService;

        public FileUploadController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [Route("file/upload/{fileId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(string fileId)
        {
            var errorMessage = await _imageService.ReadFromHttp(Request.Content, fileId);

            if (errorMessage != null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    errorMessage
                );
            }

            return Request.CreateResponse(HttpStatusCode.OK, fileId);
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Controllers
{
    public class ThumbnailController : ApiController
    {
        readonly IImageService _imageService;
        public ThumbnailController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [Route("thumbnail/{fileId}/{size}")]
        [HttpGet]
        public HttpResponseMessage GetThumbnail(string fileId, string size)
        {
            // todo: json converter
            var typedId = new FileId(fileId);
            
            var imageDescriptor = _imageService.GetImageDescriptor(typedId, size);
            if (imageDescriptor == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Image not found");
            
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(imageDescriptor.OpenRead());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(imageDescriptor.ContentType);
            return response;
        }
    }
}

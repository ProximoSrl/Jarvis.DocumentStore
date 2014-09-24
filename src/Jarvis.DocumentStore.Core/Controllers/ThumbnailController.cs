using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;

namespace Jarvis.DocumentStore.Core.Controllers
{
    public class ThumbnailController : ApiController
    {
        readonly IFileService _fileService;
        public ThumbnailController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [Route("thumbnail/{fileId}/{size}")]
        [HttpGet]
        public HttpResponseMessage GetThumbnail(FileId fileId, string size)
        {
            var imageDescriptor = _fileService.GetImageDescriptor(fileId, size);
            if (imageDescriptor == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Image not found");
            
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(imageDescriptor.OpenRead());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(imageDescriptor.ContentType);
            return response;
        }
    }
}

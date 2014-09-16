using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Controllers
{
    public class ThumbnailController : ApiController
    {
        public ThumbnailController(IFileStore fileStore, IFileInfoService fileInfoService)
        {
            FileInfoService = fileInfoService;
            FileStore = fileStore;
        }

        private IFileStore FileStore { get; set; }
        private IFileInfoService FileInfoService { get; set; }

        [Route("thumbnail/status")]
        [HttpGet]
        public string Status()
        {
            return "ok";
        }


        [Route("thumbnail/upload/{id}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(string id)
        {
            if (Request.Content == null || !Request.Content.IsMimeMultipartContent())
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Attachment not found!"
                );
            }

            var provider = new FileStoreMultipartStreamProvider(FileStore, id);

            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);
            }
            catch (UnsupportedFileFormat ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.UnsupportedMediaType,
                    ex.Message
                );
            }

            FileInfoService.Create(id, provider.Filename);

            return Request.CreateResponse(HttpStatusCode.OK, id);
        }

        [Route("thumbnail/{id}/{size}")]
        [HttpGet]
        public HttpResponseMessage GetThumbnail(string id, string size)
        {
            var descriptor = FileStore.GetDescriptor(id + "/thumbnail/" + size);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(descriptor.OpenRead());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(descriptor.ContentType);
            return response;
        }
    }
}

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
        public ThumbnailController(
            IFileStore fileStore, 
            IFileInfoService fileInfoService,
            ConfigService config
        )
        {
            FileInfoService = fileInfoService;
            FileStore = fileStore;
            Config = config;
        }

        private IFileStore FileStore { get; set; }
        private IFileInfoService FileInfoService { get; set; }
        private ConfigService Config { get; set; }

        [Route("thumbnail/status")]
        [HttpGet]
        public string Status()
        {
            return "ok";
        }

        [Route("thumbnail/upload/{fileId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(string fileId)
        {
            if (Request.Content == null || !Request.Content.IsMimeMultipartContent())
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Attachment not found!"
                );
            }

            var provider = new FileStoreMultipartStreamProvider(FileStore, fileId);
            await Request.Content.ReadAsMultipartAsync(provider);

            if (provider.Filename == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Attachment not found!"
                );
            }

            if (provider.UnsupportedExtension != null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    string.Format("Unsupported file {0}", provider.UnsupportedExtension)
                );
            }

            FileInfoService.Create(
                fileId, 
                provider.Filename,
                Config.GetDefaultSizes()
            );

            return Request.CreateResponse(HttpStatusCode.OK, fileId);
        }

        [Route("thumbnail/{fileId}/{size}")]
        [HttpGet]
        public HttpResponseMessage GetThumbnail(string fileId, string size)
        {
            var descriptor = FileStore.GetDescriptor(fileId + "/thumbnail/" + size);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(descriptor.OpenRead());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(descriptor.ContentType);
            return response;
        }
    }
}

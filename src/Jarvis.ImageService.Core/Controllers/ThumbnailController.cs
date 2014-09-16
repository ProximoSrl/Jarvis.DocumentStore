using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.ProcessinPipeline;
using Jarvis.ImageService.Core.Storage;
using Quartz;

namespace Jarvis.ImageService.Core.Controllers
{
    public class ThumbnailController : ApiController
    {
        public ThumbnailController(IFileStore fileStore, IPipelineScheduler pipelineScheduler)
        {
            PipelineScheduler = pipelineScheduler;
            FileStore = fileStore;
        }

        private IFileStore FileStore { get; set; }
        private IPipelineScheduler PipelineScheduler { get; set; }

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
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var provider = new FileStoreMultipartStreamProvider(FileStore, id);
            await Request.Content.ReadAsMultipartAsync(provider);

            PipelineScheduler.QueueThumbnail(id);

            return Request.CreateResponse(HttpStatusCode.OK, id);
        }

        [Route("thumbnail/{id}/{size}")]
        [HttpGet]
        public HttpResponseMessage GetThumbnail(string id, string size)
        {
            var descriptor = FileStore.GetDescriptor(id + "/thumbnail/"+size);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(descriptor.OpenRead());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(descriptor.ContentType);
            return response;
        }
    }
}

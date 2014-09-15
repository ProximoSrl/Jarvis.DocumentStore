using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Controllers
{
    public class ThumbnailController : ApiController
    {
        public ThumbnailController(IFileStore fileStore)
        {
            FileStore = fileStore;
        }

        private IFileStore FileStore { get; set; }

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

            return Request.CreateResponse(HttpStatusCode.OK, "Created @ " + DateTime.Now);
        }
    }
}

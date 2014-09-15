using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Host.Controllers
{
    public class ThumbnailController : ApiController
    {
        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }

        [Route("thumbnail/upload")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var provider = new FileStoreMultipartStreamProvider(FileStore);
            await Request.Content.ReadAsMultipartAsync(provider);

            return Request.CreateResponse(HttpStatusCode.OK, "Created @ " + DateTime.Now);
        }
    }
}

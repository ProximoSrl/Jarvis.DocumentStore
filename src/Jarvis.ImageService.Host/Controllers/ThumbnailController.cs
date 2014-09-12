using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;

namespace Jarvis.ImageService.Host.Controllers
{
    public class ThumbnailController : ApiController
    {
        public ILogger Logger { get; set; }
        
        [Route("thumbnail/upload")]
        [HttpPost]
        public string Upload()
        {
            return "Created @ " + DateTime.Now;
        }
    }
}

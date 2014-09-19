using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.Services;

namespace Jarvis.ImageService.Core.Controllers
{
    public class FileUploadController : ApiController
    {
        readonly IFileService _fileService;

        public FileUploadController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [Route("file/upload/{fileId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(FileId fileId)
        {
            var errorMessage = await _fileService.UploadFromHttpContent(Request.Content, fileId);

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

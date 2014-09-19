using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Services;

namespace Jarvis.ImageService.Core.Controllers
{
    public class FileUploadController : ApiController
    {
        readonly IFileService _fileService;
        readonly IConversionWorkflow _conversionWorkflow;
        public FileUploadController(IFileService fileService, IConversionWorkflow conversionWorkflow)
        {
            _fileService = fileService;
            _conversionWorkflow = conversionWorkflow;
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

            _conversionWorkflow.Start(fileId);

            return Request.CreateResponse(HttpStatusCode.OK, fileId);
        }
    }
}

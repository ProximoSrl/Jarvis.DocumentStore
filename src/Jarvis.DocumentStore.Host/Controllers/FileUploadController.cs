using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Providers;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class FileUploadController : ApiController
    {
        readonly IFileStore _fileStore;
        readonly IConversionWorkflow _conversionWorkflow;
        readonly ConfigService _configService;
        public FileUploadController(IConversionWorkflow conversionWorkflow, IFileStore fileStore, ConfigService configService)
        {
            _conversionWorkflow = conversionWorkflow;
            _fileStore = fileStore;
            _configService = configService;
        }

        [Route("file/upload/{fileId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(FileId fileId)
        {
            var errorMessage = await UploadFromHttpContent(Request.Content, fileId);

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

        /// <summary>
        /// Upload a file sent in an http request
        /// </summary>
        /// <param name="httpContent">request's content</param>
        /// <param name="fileId">Id of the new file</param>
        /// <returns>Error message or null</returns>
        private async Task<string> UploadFromHttpContent(HttpContent httpContent, FileId fileId)
        {
            if (httpContent == null || !httpContent.IsMimeMultipartContent())
            {
                return "Attachment not found!";
            }

            var provider = await httpContent.ReadAsMultipartAsync(
                new FileStoreMultipartStreamProvider(_fileStore, fileId, _configService)
            );

            if (provider.Filename == null)
            {
                return "Attachment not found!";
            }

            if (provider.IsInvalidFile)
            {
                return string.Format("Unsupported file {0}", provider.Filename);
            }

            return null;
        }
    }
}

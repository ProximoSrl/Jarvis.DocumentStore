using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
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
        readonly ConfigService _configService;
        readonly ICommandBus _commandBus;
        readonly IIdentityGenerator _identityGenerator;

        public ILogger Logger { get; set; }

        public FileUploadController(IFileStore fileStore, ConfigService configService, ICommandBus commandBus, IIdentityGenerator identityGenerator)
        {
            _fileStore = fileStore;
            _configService = configService;
            _commandBus = commandBus;
            _identityGenerator = identityGenerator;
        }

        [Route("file/upload/status")]
        [HttpGet]
        public string Status()
        {
            return "ok";
        }

        [Route("file/upload/{fileId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(FileId fileId)
        {
            Logger.DebugFormat("Incoming file {0}", fileId);
            var errorMessage = await UploadFromHttpContent(Request.Content, fileId);
            Logger.DebugFormat("File {0} processed with message {1}", fileId, errorMessage);

            if (errorMessage != null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    errorMessage
                );
            }

            var documentId = _identityGenerator.New<DocumentId>();
            _commandBus.SendLocal(new CreateDocument(documentId, fileId), "ds");

            Logger.DebugFormat("File {0} uploaded as {1}", fileId, documentId);

            return Request.CreateResponse(HttpStatusCode.OK, documentId);
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

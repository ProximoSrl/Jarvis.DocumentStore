using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Providers;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class FileController : ApiController
    {
        readonly IFileStore _fileStore;
        readonly ConfigService _configService;
        readonly ICommandBus _commandBus;
        readonly IIdentityGenerator _identityGenerator;
        readonly IReader<AliasToDocument, FileAlias> _aliasToDocument;
        readonly IReader<DocumentReadModel, DocumentId> _documentReader;
        public ILogger Logger { get; set; }
        FileNameWithExtension _fileName;


        public FileController(IFileStore fileStore, ConfigService configService, ICommandBus commandBus, IIdentityGenerator identityGenerator, IReader<AliasToDocument, FileAlias> aliasToDocument, IReader<DocumentReadModel, DocumentId> documentReader)
        {
            _fileStore = fileStore;
            _configService = configService;
            _commandBus = commandBus;
            _identityGenerator = identityGenerator;
            _aliasToDocument = aliasToDocument;
            _documentReader = documentReader;
        }

        [HttpGet]
        [Route("status")]
        public string GetStatus()
        {
            return DateTime.UtcNow.ToLongTimeString();
        }

        [Route("file/upload/{alias}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(FileAlias alias)
        {
            var documentId = _identityGenerator.New<DocumentId>();

            Logger.DebugFormat("Incoming file {0}, assigned {1}", alias, documentId);
            var fileId = new FileId(documentId);
            var errorMessage = await UploadFromHttpContent(Request.Content, fileId);
            Logger.DebugFormat("File {0} processed with message {1}", fileId, errorMessage);

            if (errorMessage != null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    errorMessage
                );
            }

            _commandBus.Send(new CreateDocument(documentId, fileId, alias, _fileName));

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
                return "Attachment not found!";

            var provider = await httpContent.ReadAsMultipartAsync(
                new FileStoreMultipartStreamProvider(_fileStore, fileId, _configService)
            );

            if (provider.Filename == null)
                return "Attachment not found!";

            if (provider.IsInvalidFile)
                return string.Format("Unsupported file {0}", provider.Filename);

            _fileName = provider.Filename;

            return null;
        }

        [Route("file/{alias}/{format}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetFormat(FileAlias alias, DocumentFormat format)
        {
            var mapping = _aliasToDocument.FindOneById(alias);
            if (mapping == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Document not found for alias {0}", alias)
                );
            }

            var document = _documentReader.FindOneById(mapping.DocumentId);
            if (document == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Document {0} not found", mapping.DocumentId)
                );
            }

            if (format == DocumentFormats.Original)
            {
                return StreamFile(
                    document.GetFormatFileId(format), 
                    document.GetFileName(alias)
                );
            }

            FileId formatFileId = document.GetFormatFileId(format);
            if (formatFileId == FileId.Null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Document {0} doesn't have format {1}",
                        alias,
                        format
                    )
                );
            }

            return StreamFile(formatFileId);
        }

        HttpResponseMessage StreamFile(FileId formatFileId, FileNameWithExtension fileName = null)
        {
            var descriptor = _fileStore.GetDescriptor(formatFileId);

            if (descriptor == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("File {0} not found",formatFileId)
                );
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(descriptor.OpenRead());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(descriptor.ContentType);

            if (fileName != null)
            {
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
            }

            return response;
        }
    }
}

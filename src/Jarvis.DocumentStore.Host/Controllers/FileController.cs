using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using CQRS.Kernel.Store;
using CQRS.Shared.Commands;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
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
        readonly IIdentityGenerator _identityGenerator;
        readonly IReader<HandleToDocument, FileHandle> _handleToDocument;
        readonly IReader<DocumentReadModel, DocumentId> _documentReader;
        public ILogger Logger { get; set; }
        FileNameWithExtension _fileName;
        readonly ICQRSRepository _repository;

        public FileController(IFileStore fileStore, ConfigService configService, IIdentityGenerator identityGenerator, IReader<HandleToDocument, FileHandle> handleToDocument, IReader<DocumentReadModel, DocumentId> documentReader, ICQRSRepository repository)
        {
            _fileStore = fileStore;
            _configService = configService;
            _identityGenerator = identityGenerator;
            _handleToDocument = handleToDocument;
            _documentReader = documentReader;
            _repository = repository;
        }

        [Route("file/upload/{handle}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(FileHandle handle)
        {
            var documentId = _identityGenerator.New<DocumentId>();

            Logger.DebugFormat("Incoming file {0}, assigned {1}", handle, documentId);
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

            CreateDocument(documentId, fileId, handle, _fileName);

            Logger.DebugFormat("File {0} uploaded as {1}", fileId, documentId);

            return Request.CreateResponse(HttpStatusCode.OK, documentId);
        }

        private void CreateDocument(DocumentId documentId, FileId fileId, FileHandle handle, FileNameWithExtension fileName)
        {
            Thread.CurrentPrincipal = new GenericPrincipal(
                new GenericIdentity("api"), new string[] { }
            );

            var document = new Document();
            document.Create(documentId, fileId, handle, fileName);
            this._repository.Save(document, Guid.NewGuid(), d => { });
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

        [Route("file/{handle}/{format?}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetFormat(FileHandle handle, DocumentFormat format = null)
        {
            var mapping = _handleToDocument.FindOneById(handle);
            if (mapping == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Document not found for handle {0}", handle)
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

            if (format == null)
            {
                var formats = document.Formats.ToDictionary(x => 
                    (string) x.Key, 
                    x => Url.Content("/file/"+handle+"/"+x.Key)
                );
                return Request.CreateResponse(HttpStatusCode.OK, formats);
            }

            if (format == DocumentFormats.Original)
            {
                return StreamFile(
                    document.GetFormatFileId(format),
                    document.GetFileName(handle)
                );
            }

            FileId formatFileId = document.GetFormatFileId(format);
            if (formatFileId == FileId.Null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Document {0} doesn't have format {1}",
                        handle,
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
                    string.Format("File {0} not found", formatFileId)
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

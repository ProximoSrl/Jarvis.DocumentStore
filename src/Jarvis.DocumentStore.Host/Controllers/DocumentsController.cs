using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using CQRS.Kernel.Commands;
using CQRS.Kernel.Store;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Domain.Handle.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Model;
using Jarvis.DocumentStore.Host.Providers;
using Newtonsoft.Json;
using Jarvis.DocumentStore.Shared;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class DocumentsController : ApiController, ITenantController
    {
        readonly IBlobStore _blobStore;
        readonly ConfigService _configService;
        readonly IIdentityGenerator _identityGenerator;
        readonly IReader<DocumentReadModel, DocumentId> _documentReader;
        public ILogger Logger { get; set; }
        public IInProcessCommandBus CommandBus { get; private set; }
        readonly IHandleWriter _handleWriter;

        HandleCustomData _customData;
        FileNameWithExtension _fileName;
        BlobId _blobId;

        public DocumentsController(
            IBlobStore blobStore,
            ConfigService configService,
            IIdentityGenerator identityGenerator,
            IReader<DocumentReadModel, DocumentId> documentReader,
            IInProcessCommandBus commandBus,
            IHandleWriter handleWriter
        )
        {
            _blobStore = blobStore;
            _configService = configService;
            _identityGenerator = identityGenerator;
            _documentReader = documentReader;
            CommandBus = commandBus;
            _handleWriter = handleWriter;
        }

        [Route("{tenantId}/documents/{handle}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(TenantId tenantId, DocumentHandle handle)
        {
            var documentId = _identityGenerator.New<DocumentId>();

            Logger.DebugFormat("Incoming file {0}, assigned {1}", handle, documentId);
            var errorMessage = await UploadFromHttpContent(Request.Content);
            Logger.DebugFormat("File {0} processed with message {1}", _blobId, errorMessage);

            if (errorMessage != null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    errorMessage
                );
            }

            CreateDocument(documentId, _blobId, handle, _fileName, _customData);

            Logger.DebugFormat("File {0} uploaded as {1}", _blobId, documentId);

            var storedFile = _blobStore.GetDescriptor(_blobId);

            return Request.CreateResponse(
                HttpStatusCode.OK,
                new UploadedDocumentResponse
                {
                    Handle = handle,
                    Hash = storedFile.Hash,
                    HashType = "md5",
                    Uri = Url.Content("/" + tenantId + "/documents/" + handle)
                }
            );
        }

        [Route("{tenantId}/documents/addformat/{format}")]
        [HttpPost]
        public async Task<HttpResponseMessage> AddFormatToDocument(TenantId tenantId, DocumentFormat format)
        {
            var errorMessage = await AddFormatFromHttpContent(Request.Content, format);
            Logger.DebugFormat("File {0} processed with message {1}", _blobId, errorMessage);

            if (errorMessage != null)
            {
                Logger.Error("Error Adding format To Document: " + errorMessage);
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    errorMessage
                );
            }

            var docIdParameter = _customData[AddFormatToDocumentParameters.DocumentId] as String;
            DocumentId documentId;
            if (docIdParameter == null)
            {
                //user ask for handle, we need to grab the handle
                var documentHandle = new DocumentHandle(_customData[AddFormatToDocumentParameters.DocumentHandle] as String);
                var handle = _handleWriter.FindOneById(documentHandle);
                documentId = handle.DocumentId;
            }
            else
            {
                documentId = new DocumentId(_customData[AddFormatToDocumentParameters.DocumentId] as String);
            }

            var documentFormat = new DocumentFormat(_customData[AddFormatToDocumentParameters.Format] as String);
            var createdById = new PipelineId(_customData[AddFormatToDocumentParameters.CreatedBy] as String);
            Logger.DebugFormat("Incoming new format for documentId {0}", documentId);

            var command = new AddFormatToDocument(documentId, documentFormat, _blobId, createdById);
            CommandBus.Send(command, "api");

            return Request.CreateResponse(
                HttpStatusCode.OK,
                new AddFormatToDocumentResponse
                {
                    Result = true,
                }
            );
        }

        /// <summary>
        /// Upload a file sent in an http request
        /// </summary>
        /// <param name="httpContent">request's content</param>
        /// <returns>Error message or null</returns>
        private async Task<String> UploadFromHttpContent(HttpContent httpContent)
        {
            if (httpContent == null || !httpContent.IsMimeMultipartContent())
                return "Attachment not found!";

            var provider = await httpContent.ReadAsMultipartAsync(
                new FileStoreMultipartStreamProvider(_blobStore, _configService)
            );

            if (provider.Filename == null)
                return "Attachment not found!";

            if (provider.IsInvalidFile)
                return string.Format("Unsupported file {0}", provider.Filename);

            if (provider.FormData["custom-data"] != null)
            {
                _customData = JsonConvert.DeserializeObject<HandleCustomData>(provider.FormData["custom-data"]);
            }

            _fileName = provider.Filename;
            _blobId = provider.BlobId;
            return null;
        }

        /// <summary>
        /// Upload a file sent in an http request
        /// </summary>
        /// <param name="httpContent">request's content</param>
        /// <returns>Error message or null</returns>
        private async Task<String> AddFormatFromHttpContent(HttpContent httpContent, DocumentFormat format)
        {
            if (httpContent == null || !httpContent.IsMimeMultipartContent())
                return "Attachment not found!";

            var provider = await httpContent.ReadAsMultipartAsync(
                new FormatStoreMultipartStreamProvider(_blobStore, format)
            );

            if (provider.Filename == null)
                return "Attachment not found!";

            if (provider.FormData["custom-data"] != null)
            {
                _customData = JsonConvert.DeserializeObject<HandleCustomData>(provider.FormData["custom-data"]);
            }

            _fileName = provider.Filename;
            _blobId = provider.BlobId;
            return null;
        }

        [Route("{tenantId}/documents/{handle}/@customdata")]
        [HttpGet]
        public HttpResponseMessage GetCustomData(TenantId tenantId, DocumentHandle handle)
        {
            var data = _handleWriter.FindOneById(handle);
            if (data == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Document not found");

            return Request.CreateResponse(HttpStatusCode.OK, data.CustomData);
        }

        [Route("{tenantId}/documents/{handle}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetFormatList(
            TenantId tenantId,
            DocumentHandle handle
        )
        {
            var document = GetDocumentByHandle(handle);

            if (document == null)
            {
                return DocumentNotFound(handle);
            }

            var formats = document.Formats.ToDictionary(x =>
                (string)x.Key,
                x => Url.Content("/" + tenantId + "/documents/" + handle + "/" + x.Key)
            );
            return Request.CreateResponse(HttpStatusCode.OK, formats);
        }

        [Route("{tenantId}/documents/{handle}/{format}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetFormat(
            TenantId tenantId,
            DocumentHandle handle,
            DocumentFormat format
        )
        {
            var mapping = _handleWriter.FindOneById(handle);
            if (mapping == null)
                return DocumentNotFound(handle);

            var document = _documentReader.FindOneById(mapping.DocumentId);

            if (document == null)
            {
                return DocumentNotFound(handle);
            }

            if (format == DocumentFormats.Original)
            {
                return StreamFile(
                    document.GetFormatBlobId(format),
                    mapping.FileName
                );
            }

            BlobId formatBlobId = document.GetFormatBlobId(format);
            if (formatBlobId == BlobId.Null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Document {0} doesn't have format {1}",
                        handle,
                        format
                    )
                );
            }

            return StreamFile(formatBlobId);
        }

        [Route("{tenantId}/documents/blobs/{blobId}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetBlob(
            TenantId tenantId,
            BlobId blobId
        )
        {
            return StreamFile(blobId);
        }

        HttpResponseMessage DocumentNotFound(DocumentHandle handle)
        {
            return Request.CreateErrorResponse(
                HttpStatusCode.NotFound,
                string.Format("Document {0} not found", handle)
                );
        }

        HttpResponseMessage StreamFile(BlobId formatBlobId, FileNameWithExtension fileName = null)
        {
            var descriptor = _blobStore.GetDescriptor(formatBlobId);

            if (descriptor == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("File {0} not found", formatBlobId)
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

        [HttpDelete]
        [Route("{tenantId}/documents/{handle}")]
        public HttpResponseMessage DeleteFile(TenantId tenantId, DocumentHandle handle)
        {
            var document = GetDocumentByHandle(handle);
            if (document == null)
                return DocumentNotFound(handle);

            CommandBus.Send(new DeleteHandle(handle), "api");

            return Request.CreateResponse(
                HttpStatusCode.Accepted,
                string.Format("Document marked for deletion {0}", handle)
            );
        }


        private void CreateDocument(
            DocumentId documentId,
            BlobId blobId,
            DocumentHandle handle,
            FileNameWithExtension fileName,
            HandleCustomData customData
        )
        {
            var handleInfo = new DocumentHandleInfo(handle, fileName, customData);
            var descriptor = _blobStore.GetDescriptor(blobId);
            var createDocument = new CreateDocument(documentId, blobId, handleInfo, descriptor.Hash, fileName);
            //            createDocument.WithDiagnosticDescription("Created by rest api");
            CommandBus.Send(createDocument, "api");
        }

        DocumentReadModel GetDocumentByHandle(DocumentHandle handle)
        {
            var mapping = _handleWriter.FindOneById(handle);
            if (mapping == null)
                return null;

            return _documentReader.FindOneById(mapping.DocumentId);
        }
    }


}

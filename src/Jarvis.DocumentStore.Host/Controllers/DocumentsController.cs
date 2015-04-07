using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Model;
using Jarvis.DocumentStore.Host.Providers;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using Newtonsoft.Json;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;
using DocumentHandle = Jarvis.DocumentStore.Core.Model.DocumentHandle;
using Jarvis.Framework.Shared.Commands;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class DocumentsController : ApiController, ITenantController
    {
        readonly IBlobStore _blobStore;
        readonly DocumentStoreConfiguration _configService;
        readonly IIdentityGenerator _identityGenerator;
        private readonly ICounterService _counterService;
        private IDocumentFormatTranslator _documentFormatTranslator;

        readonly IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentDescriptorReader;
        readonly IQueueDispatcher _queueDispatcher;

        public ILogger Logger { get; set; }
        public IInProcessCommandBus CommandBus { get; private set; }
        readonly IDocumentWriter _handleWriter;

        DocumentCustomData _customData;
        FileNameWithExtension _fileName;
        BlobId _blobId;

        public DocumentsController(
            IBlobStore blobStore,
            DocumentStoreConfiguration configService,
            IIdentityGenerator identityGenerator,
            IReader<DocumentDescriptorReadModel, DocumentDescriptorId> documentDescriptorReader,
            IInProcessCommandBus commandBus,
            IDocumentWriter handleWriter,
            IQueueDispatcher queueDispatcher, 
            ICounterService counterService,
            IDocumentFormatTranslator documentFormatTranslator)
        {
            _blobStore = blobStore;
            _configService = configService;
            _identityGenerator = identityGenerator;
            _documentDescriptorReader = documentDescriptorReader;
            _handleWriter = handleWriter;
            _queueDispatcher = queueDispatcher;
            _counterService = counterService;
            _documentFormatTranslator = documentFormatTranslator;
            CommandBus = commandBus;
        }

        [Route("{tenantId}/documents/{handle}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload(TenantId tenantId, DocumentHandle handle)
        {
            return await InnerUploadDocument(tenantId, handle);
        }


        /// <summary>
        /// Upload a new document but it will be considered an attached of an existing
        /// handle.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="fatherHandle">Handle that will receive the attachment</param>
        /// <param name="attachSource">Source of attach, it can be "zip" to indicate the queue that unzip document.
        /// This will be translated into an unique id from the conttroller.</param>
        /// <returns></returns>
        [Route("{tenantId}/documents/{fatherHandle}/attach/{attachSource}")]
        [HttpPost]
        public async Task<HttpResponseMessage> UploadAttach(TenantId tenantId, DocumentHandle fatherHandle, DocumentHandle attachSource)
        {
            var realAttacHandle = GetAttachHandleFromAttachSource(attachSource);
            return await InnerUploadDocument(tenantId, realAttacHandle, fatherHandle);
        }

        private DocumentHandle GetAttachHandleFromAttachSource(String attachSource)
        {
            var realAttacHandle = new DocumentHandle(attachSource + "_" + _counterService.GetNext(attachSource));
            return realAttacHandle;
        }

        /// <summary>
        /// Upload a new document but it will be considered an attached of an existing
        /// handle.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="queueName"></param>
        /// <param name="jobId"></param>
        /// <param name="attachSource">Source of attach, it can be "zip" to indicate the queue that unzip document.
        /// This will be translated into an unique id from the conttroller.</param>
        /// <returns></returns>
        [Route("{tenantId}/documents/jobs/attach/{queueName}/{jobId}/{attachSource}")]
        [HttpPost]
        public async Task<HttpResponseMessage> UploadAttachFromJob(TenantId tenantId, String queueName, String jobId, String attachSource)
        {
            var job = _queueDispatcher.GetJob(queueName, jobId);
            if (job == null)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    String.Format("Job id {0} not valid for queue {1}", jobId, queueName)
                );
            }
            var realAttacHandle = GetAttachHandleFromAttachSource(attachSource);
            return await InnerUploadDocument(tenantId, realAttacHandle, job.Handle);
        }

        private async Task<HttpResponseMessage> InnerUploadDocument(TenantId tenantId, DocumentHandle handle, DocumentHandle fatherHandle = null)
        {
            var documentId = _identityGenerator.New<DocumentDescriptorId>();

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

            CreateDocument(documentId, _blobId, handle, fatherHandle, _fileName, _customData);

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

            String queueName = _customData[AddFormatToDocumentParameters.QueueName] as String;
            String jobId = _customData[AddFormatToDocumentParameters.JobId] as String;
            DocumentDescriptorId documentId;
            if (String.IsNullOrEmpty(queueName))
            {
                //user ask for handle, we need to grab the handle
                var documentHandle = new DocumentHandle(_customData[AddFormatToDocumentParameters.DocumentHandle] as String);
                var handle = _handleWriter.FindOneById(documentHandle);
                documentId = handle.DocumentDescriptorId;
                if (documentId == null)
                {
                    Logger.ErrorFormat("Trying to add a format for Handle {0} with a null DocumentId", documentHandle);
                    return Request.CreateErrorResponse(
                       HttpStatusCode.BadRequest,
                       ""
                   );
                }
                Logger.DebugFormat("Add format {0} to handle {1} and document id {2}", format, handle, documentId);
            }
            else
            {
                 var job = _queueDispatcher.GetJob(queueName, jobId);

                if (job == null) {
                    Logger.WarnFormat("Job id {0} not found in queue {1}", jobId, queueName);
                    return Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        String.Format("Job id {0} not found in queue {1}", jobId, queueName));
                }
                documentId = job.DocumentId;
                if (documentId == null)
                {
                    Logger.ErrorFormat("Trying to add a format for Job Id {0} queue {1} - Job has DocumentId null", jobId, queueName);
                    return Request.CreateErrorResponse(
                       HttpStatusCode.BadRequest,
                       ""
                   );
                }
                Logger.DebugFormat("Add format {0} to job id {1} and document id {2}", format, job.Id, documentId);
            }

            if (format == "null") 
            {
                var formatFromFileName = _documentFormatTranslator.GetFormatFromFileName(_fileName);
                if (formatFromFileName == null)
                {
                    String error = "Format not specified and no known format for file: " + _fileName;
                    Logger.Error(error);
                    return Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        error
                    );
                }
                format = new DocumentFormat(formatFromFileName); 
            }

            var createdById = new PipelineId(_customData[AddFormatToDocumentParameters.CreatedBy] as String);
            Logger.DebugFormat("Incoming new format for documentId {0}", documentId);

            var command = new AddFormatToDocumentDescriptor(documentId, format, _blobId, createdById);
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
                _customData = JsonConvert.DeserializeObject<DocumentCustomData>(provider.FormData["custom-data"]);
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
                _customData = JsonConvert.DeserializeObject<DocumentCustomData>(provider.FormData["custom-data"]);
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

        [Route("{tenantId}/documents/attachments/{handle}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetAttachmentList(
            TenantId tenantId,
            DocumentHandle handle
        )
        {
            var document = _handleWriter.FindOneById(handle);

            if (document == null)
            {
                return DocumentNotFound(handle);
            }

            if (document.Attachments == null) return Request.CreateResponse(HttpStatusCode.OK, new Dictionary<DocumentHandle, Uri>());

            var attachments = document.Attachments.ToDictionary(x =>
                x,
                x => Url.Content("/" + tenantId + "/documents/" + x)
            );
            return Request.CreateResponse(HttpStatusCode.OK, attachments);
        }

        /// <summary>
        /// Retrieve all attachments for the document
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        [Route("{tenantId}/documents/attachments_fat/{handle}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetAttachmentFat(
            TenantId tenantId,
            DocumentHandle handle
        )
        {
            var document = _handleWriter.FindOneById(handle);

            if (document == null)
            {
                return DocumentNotFound(handle);
            }

            if (document.Attachments == null) return Request.CreateResponse(HttpStatusCode.OK, new Dictionary<DocumentHandle, Uri>());
            List<DocumentAttachmentsFat.AttachmentInfo> fat = new List<DocumentAttachmentsFat.AttachmentInfo>();

            ScanAttachments(tenantId, document, fat, "");

            return Request.CreateResponse(HttpStatusCode.OK, fat);
        }

        private void ScanAttachments(TenantId tenantId, DocumentReadModel document, List<DocumentAttachmentsFat.AttachmentInfo> fat, String attachmentPath)
        {
            foreach (var attachment in document.Attachments)
            {
                var attachmentDocument = _handleWriter.FindOneById(attachment);
                String relativePath = "";
                if (attachmentDocument.CustomData.ContainsKey(JobsConstants.AttachmentRelativePath))
                {
                    relativePath = attachmentDocument.CustomData[JobsConstants.AttachmentRelativePath] as String;
                    relativePath = relativePath.TrimStart('\\').Replace("\\", "/");
                    if (attachmentDocument.FileName != null)
                    {
                        if (relativePath.EndsWith(attachmentDocument.FileName))
                        {
                            relativePath = relativePath.Substring(0, relativePath.Length - attachmentDocument.FileName.ToString().Length)
                                .TrimEnd('/');
                        }
                    }
                }
                fat.Add(new DocumentAttachmentsFat.AttachmentInfo(
                        Url.Content("/" + tenantId + "/documents/" + attachment),
                        attachmentDocument.FileName,
                        attachmentPath,
                        relativePath
                    ));
               
                if (attachmentDocument.Attachments != null && attachmentDocument.Attachments.Count > 0)
                    ScanAttachments(tenantId, attachmentDocument, fat, attachmentPath + "/" + attachmentDocument.FileName);
            }
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

            var document = _documentDescriptorReader.FindOneById(mapping.DocumentDescriptorId);

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

        /// <summary>
        /// Gets a blob given job id
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="jobId"></param>
        /// <param name="queueName">Name of the queue</param>
        /// <returns></returns>
        [Route("{tenantId}/documents/jobs/blob/{queueName}/{jobId}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetBlobForJob(
            TenantId tenantId,
            String queueName,
            String jobId
        )
        {
            var job = _queueDispatcher.GetJob(queueName, jobId);
            if (job == null) 
                return Request.CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    string.Format("Job {0} not found", jobId)
                    ); ;

            return StreamFile(job.BlobId);
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

            CommandBus.Send(new DeleteDocument(handle), "api");

            return Request.CreateResponse(
                HttpStatusCode.Accepted,
                string.Format("Document marked for deletion {0}", handle)
            );
        }

        [HttpDelete]
        [Route("{tenantId}/documents/attachments/{fatherHandle}/{source}")]
        public HttpResponseMessage DeleteAttachments(TenantId tenantId, DocumentHandle fatherHandle, String source)
        {
            var document = GetDocumentByHandle(fatherHandle);
            if (document == null)
                return DocumentNotFound(fatherHandle);

            //find all the attachments belonging to this source
            CommandBus.Send(new DeleteAttachments(fatherHandle, source), "api");

            return Request.CreateResponse(
                HttpStatusCode.Accepted,
                string.Format("Attachment of handle {0} with source {1} marked for deletion!", fatherHandle, source)
            );
        }



        private void CreateDocument(
            DocumentDescriptorId documentId,
            BlobId blobId,
            DocumentHandle handle,
            DocumentHandle fatherHandle,
            FileNameWithExtension fileName,
            DocumentCustomData customData
        )
        {
            
            var descriptor = _blobStore.GetDescriptor(blobId);
            ICommand createDocument;
            var handleInfo = new DocumentHandleInfo(handle, fileName, customData);
            if (fatherHandle == null)
            {
                
                createDocument = new InitializeDocumentDescriptor(documentId, blobId, handleInfo, descriptor.Hash, fileName);
            }
            else
            {
                createDocument = new InitializeDocumentDescriptorAsAttach(documentId, blobId, handleInfo, fatherHandle, descriptor.Hash, fileName);
            }
            CommandBus.Send(createDocument, "api");
        }

        DocumentDescriptorReadModel GetDocumentByHandle(DocumentHandle handle)
        {
            var mapping = _handleWriter.FindOneById(handle);
            //check if handle is not present, or if the handle still missing descriptor (still not deduplicated)
            if (mapping == null || mapping.DocumentDescriptorId == null)
                return null;

            return _documentDescriptorReader.FindOneById(mapping.DocumentDescriptorId);
        }
    }


}

using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.Framework.Shared.MultitenantSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.Framework.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using System.Net;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using System.Net.Http.Headers;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class RecycleBinController : ApiController, ITenantController
    {
        public IRecycleBin _recycleBin { get; set; }
        readonly IBlobStore _blobStore;

        public RecycleBinController(
                IRecycleBin recycleBin,
                IBlobStore blobStore
            )
        {
            _recycleBin = recycleBin;
            _blobStore = blobStore;
        }

        [Route("{tenantId}/recyclebin/documents")]
        [HttpPost]
        public RecycleBinResponse GetRecycleBin(
                TenantId tenantId,
                RecycleBinRequest request
            )
        {
            var page = request.Page - 1;
            var start = page * request.PageSize;
            var recycledDocuments = _recycleBin.Slots
                .Where(s => s.Id.StreamId.StartsWith("Document_"))
                .OrderByDescending(s => s.DeletedAt)
                .Skip(start)
                .Take(request.PageSize)
                .ToList()
                .Select(r =>
                {
                    return new RecycleBinData()
                    {
                        Handle = r.Data["Handle"].ToString(),
                        FileName = r.Data["FileName"] as String,
                        DeletedAt = r.DeletedAt,
                        CustomData = r.Data["CustomData"],
                        DocumentId = r.Id.StreamId
                    };
                })
                .ToList();

            var count = _recycleBin.Slots
                .Where(s => s.Id.StreamId.StartsWith("Document_"))
                .Count();

            return new RecycleBinResponse
            {
                Documents = recycledDocuments,
                Count = count,
            };
        }

        [Route("{tenantId}/recyclebin/documents/{documentId}")]
        [HttpGet]
        [HttpHead]
        public async Task<HttpResponseMessage> GetFormat(
            TenantId tenantId,
            String documentId
        )
        {
            var slot = _recycleBin.Slots.SingleOrDefault(s => s.Id.StreamId == documentId);
            if (slot == null)
            {
                return NotFound(String.Format("Document {0} Not Found", documentId));
            }

            var fileName = slot.Data["FileName"] as String;
            var blobId = slot.Data["OriginalBlobId"] as string;
            
            return StreamFile(
                new BlobId(blobId),
                fileName ?? "blob.bin"
            );

        }

        HttpResponseMessage StreamFile(BlobId formatBlobId, String fileName = null)
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


        HttpResponseMessage NotFound(String message)
        {
            return Request.CreateErrorResponse(
                HttpStatusCode.NotFound,
                message);
        }
    }

    public class RecycleBinResponse
    {
        public Int32 Count { get; set; }

        public List<RecycleBinData> Documents { get; set; }
    }
    public class RecycleBinData
    {
        public String Handle { get; set; }

        public String FileName { get; set; }

        public DateTime DeletedAt { get; set; }

        public Object CustomData { get; set; }

        public String DocumentId { get; set; }
    }

    public class RecycleBinRequest
    {

        public int Page { get; set; }
        public int PageSize { get; set; }

        public String Filter { get; set; }

        public RecycleBinRequest()
        {
            Page = 1;
            PageSize = 30;
        }
    }
}

using Castle.Core.Logging;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Model;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class DocumentComposerController : ApiController, ITenantController
    {

        public ILogger Logger { get; set; }

        readonly IQueueManager _queueDispatcher;

        public DocumentComposerController(IQueueManager queueDispatcher)
        {
            this._queueDispatcher = queueDispatcher;
        }


        [Route("{tenantId}/compose")]
        [HttpPost]
        public async Task<HttpResponseMessage> Compose(
            TenantId tenantId,
            Model.ComposeDocumentsModel dto)
        {
            QueuedJob job = new QueuedJob();
            var id = new QueuedJobId(Guid.NewGuid().ToString());
            job.Id = id;
            job.SchedulingTimestamp = DateTime.Now;
            job.StreamId = 0;
            job.TenantId = tenantId;
            job.Parameters = new Dictionary<string, string>();
            job.Parameters.Add("documentList", String.Join<Object>("|", dto.DocumentList));
            job.Parameters.Add("resultingDocumentHandle", dto.ResultingDocumentHandle);
            job.Parameters.Add("resultingDocumentFileName", dto.ResultingDocumentFileName ?? dto.ResultingDocumentHandle);
            job.Parameters.Add(JobKeys.TenantId, tenantId);
            _queueDispatcher.QueueJob("pdfComposer", job);
            return Request.CreateResponse(
                HttpStatusCode.OK, 
                new  { result = "ok"}
            );
        }

    }
}

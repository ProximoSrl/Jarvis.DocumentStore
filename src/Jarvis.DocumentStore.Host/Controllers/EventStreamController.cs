using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.DocumentStore.Host.Model;
using Jarvis.Framework.Kernel.ProjectionEngine.Client;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson;
using NEventStore;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class EventStreamController :ApiController, ITenantController
    {
        private readonly ICommitEvents _commits;
        private CommitEnhancer _enhancer;
        public EventStreamController(IStoreEvents eventStore, CommitEnhancer enhancer)
        {
            _enhancer = enhancer;
            _commits = (ICommitEvents)eventStore;
        }

        [HttpGet]
        [Route("{tenantId}/stream/{aggregateId}")]
        public HttpResponseMessage GetStream(TenantId tenantId, string aggregateId)
        {
            if (string.IsNullOrEmpty(aggregateId))
                return null;

            var commits = _commits.GetFrom("Jarvis", aggregateId,0, int.MaxValue);

            var result = new List<CommitModel>();
            foreach (var commit in commits)
            {
                _enhancer.Enhance(commit);
                result.Add(new CommitModel(commit));
            }

            var all = result.ToJson()
                .Replace("\"_t\"", "\"Type\"")
                .Replace("ISODate(", "")
                .Replace("CSUUID(", "")
                .Replace("\")", "\"")
            ;
            
            var sc = new StringContent(all);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var resp = new HttpResponseMessage {Content = sc};
            return resp;
        }
    }
}

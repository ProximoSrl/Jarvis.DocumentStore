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
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class EventStreamController :ApiController, ITenantController
    {
        private readonly ICommitEvents _commits;
        private ICommitEnhancer _enhancer;
        private IHandleMapper _handleMapper;
        private IIdentityConverter _identityConverter;

        public EventStreamController(
            IStoreEvents eventStore, 
            ICommitEnhancer enhancer,
            IHandleMapper handleMapper,
            IIdentityConverter identityConverter)
        {
            _enhancer = enhancer;
            _commits = (ICommitEvents)eventStore;
            _handleMapper = handleMapper;
            _identityConverter = identityConverter;
        }

        [HttpGet]
        [Route("{tenantId}/stream/{aggregateId}")]
        public HttpResponseMessage GetStream(TenantId tenantId, string aggregateId)
        {
            if (string.IsNullOrEmpty(aggregateId))
                return null;

            String identity = aggregateId;
            try
            {
                _identityConverter.ToIdentity(aggregateId);
            }
            catch (Exception)
            {
                //it could be a handle
                var handle = _handleMapper.TryTranslate(aggregateId);
                if (handle == null)
                    throw new ArgumentException("Invalid aggregateId", "aggregateId");

                identity = handle.AsString();
            }
      

            var commits = _commits.GetFrom("Jarvis", identity, 0, int.MaxValue);
            
            var commitsList = new List<CommitModel>();
            foreach (var commit in commits)
            {
                _enhancer.Enhance(commit);
                commitsList.Add(new CommitModel(commit));
            }
            EventStreamResult result = new EventStreamResult();
            result.Commits = commitsList;
            result.AggregateId = identity;
             
            var all = result.ToJson()
                .Replace("\"_t\"", "\"Type\"")
                .Replace("ISODate(", "")
                .Replace("CSUUID(", "")
                .Replace("\")", "\"");          
            
            var sc = new StringContent(all);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var resp = new HttpResponseMessage {Content = sc};
            return resp;
        }
    }
}

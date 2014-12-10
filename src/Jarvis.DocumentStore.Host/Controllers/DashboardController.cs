using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class DashboardController : ApiController, ITenantController
    {
        public DashboardController(IBlobStore blobStore, IMongoDbReader<DocumentStats, string> docStats)
        {
            DocStats = docStats;
            BlobStore = blobStore;
        }

        public IBlobStore BlobStore{ get; set; }
        public IMongoDbReader<DocumentStats, string> DocStats { get; set; }
        public IHandleWriter Handles { get; set; }

        [HttpGet]
        [Route("{tenantId}/dashboard")]
        public IHttpActionResult GetStats(TenantId tenantId)
        {
            var totals = BlobStore.GetInfo();

            var aggregation = new AggregateArgs()
            {
                Pipeline = new[] { BsonDocument.Parse("{$group:{_id:1, bytes:{$sum:'$Bytes'}, documents:{$sum:'$Files'}}}") }
            };

            var result = DocStats.Aggregate(aggregation).SingleOrDefault();

            int documents = result != null ? result["documents"].AsInt32 : 0;
            long bytes = result != null ? result["bytes"].AsInt64 : 0;
            long files = totals != null ? totals.Files : 0;


            var stats = new
            {
                Tenant = tenantId,
                Documents = documents,
                DocBytes = bytes,
                Handles = Handles.Count(),
                Files = files,
                Jobs = 444
            };

            return Ok(stats);
        }
    }
}

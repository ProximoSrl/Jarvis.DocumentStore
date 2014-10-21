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
using Jarvis.DocumentStore.Core.Storage.Stats;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class DashboardController : ApiController, ITenantController
    {
        public GridFsFileStoreStats FileStoreStats { get; set; }
        public IMongoDbReader<DocumentStats, string> DocStats { get; set; }
        public IReader<HandleToDocument, DocumentHandle> Handles { get; set; }

        [HttpGet]
        [Route("{tenantId}/dashboard")]
        public IHttpActionResult GetStats(TenantId tenantId)
        {
            var totals = FileStoreStats.GetStats();

            var aggregation = new AggregateArgs()
            {
                Pipeline = new[] { BsonDocument.Parse("{$group:{_id:1, bytes:{$sum:'$Bytes'}, documents:{$sum:'$Files'}}}") }
            };

            var result = DocStats.Aggregate(aggregation).SingleOrDefault();

            int documents = result != null ? result["documents"].AsInt32 : 0;
            long bytes = result != null ? result["bytes"].AsInt64 : 0;
            int files = totals != null ? totals.Files : 0;


            var stats = new
            {
                Documents = documents,
                DocBytes = bytes,
                Handles = Handles.AllUnsorted.LongCount(),
                Files = files,
                Jobs = 444
            };

            return Ok(stats);
        }
    }
}

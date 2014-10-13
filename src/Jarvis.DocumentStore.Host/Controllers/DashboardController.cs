using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.Stats;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class DashboardController : ApiController
    {
        public GridFsFileStoreStats FileStoreStats { get; set; }
        public IMongoDbReader<DocumentStats,string> DocStats { get; set; }

        [HttpGet]
        [Route("dashboard")]
        public IHttpActionResult GetStats()
        {
            var totals = FileStoreStats.GetStats();

            var aggregation = new AggregateArgs()
            {
                Pipeline = new[] { BsonDocument.Parse("{$group:{_id:1, bytes:{$sum:'$Bytes'}, documents:{$sum:1}}}") }
            };

            var result = DocStats.Aggregate(aggregation).Single();

            var stats = new
            {
                Documents = result["documents"].AsInt32,
                DocBytes = result["bytes"].AsInt64,
                Handles = 222,
                Files = totals.Files,
                Jobs = 444
            };

            return Ok(stats);
        }
    }
}

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage.Stats
{
    public class GridFsFileStoreStats
    {
        public class Totals
        {
            public long Size { get; set; }
            public int Files { get; set; }
        }

        readonly MongoGridFS _gridFs;

        public GridFsFileStoreStats(MongoDatabase db)
        {
            _gridFs = db.GetGridFS(MongoGridFSSettings.Defaults);
        }

        public Totals GetStats()
        {
            var aggregation = new AggregateArgs()
            {
                Pipeline = new [] {BsonDocument.Parse("{$group:{_id:1, size:{$sum:'$length'}, count:{$sum:1}}}")}
            };

            var result = _gridFs.Files.Aggregate(aggregation).FirstOrDefault();
            if (result != null)
            {
                return new Totals
                {
                    Size = result["size"].AsInt64,
                    Files = result["count"].AsInt32
                };
            }

            return null;
        }
    }
}

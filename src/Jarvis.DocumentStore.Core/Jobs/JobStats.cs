using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public static class QuartzMongoConfiguration
    {
        public const string Name = "jarvis.documentstore";
    }

    public class JobStats
    {
        public sealed class TriggerStatInfo
        {
            public string Group { get; private set; }
            public string Status { get; private set; }
            public long Count { get; private set; }

            public TriggerStatInfo(string group, string status, long count)
            {
                Group = group;
                Status = status;
                Count = count;
            }
        }

        private readonly AggregateArgs _aggregation;
        private readonly MongoCollection<BsonDocument> _collection;

        public JobStats(MongoDatabase db)
        {
            _aggregation = new AggregateArgs()
            {
                Pipeline = new[] { BsonDocument.Parse("{$group : {_id : { g : '$JobGroup', s:'$State'}, c : {$sum:1}}}") }
            };
            _collection = db.GetCollection(QuartzMongoConfiguration.Name + ".Triggers");
        }

        public TriggerStatInfo[] GetTriggerStats()
        {
            return _collection.Aggregate(_aggregation).Select(x => new TriggerStatInfo(
                x["_id"]["g"].AsString, 
                x["_id"]["s"].AsString, 
                x["c"].AsInt32
                )
            ).OrderBy(x=>x.Status).ThenBy(x=>x.Group).ToArray();
        }
    }
}

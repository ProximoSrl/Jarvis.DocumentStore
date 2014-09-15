using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Jarvis.ImageService.Core.Services
{
    public interface ICounterService
    {
        long GetNext(string serie);
    }

    public class CounterService : ICounterService
    {
        readonly MongoCollection<IdentityCounter> _counters;
        public class IdentityCounter
        {
            public string Id { get; set; }
            public long Last { get; set; }
        }

        public CounterService(MongoDatabase db)
        {
            _counters = db.GetCollection<IdentityCounter>("sys.counters");
        }
        public long GetNext(string serie)
        {
            var result = _counters.FindAndModify(new FindAndModifyArgs()
            {
                Query = Query<IdentityCounter>.EQ(x => x.Id, serie),
                Update = Update<IdentityCounter>.Inc(x => x.Last, 1),
                SortBy = SortBy.Null,
                VersionReturned = FindAndModifyDocumentVersion.Modified,
                Upsert = true
            });

            var counter = result.GetModifiedDocumentAs<IdentityCounter>().Last;
            return counter;
        }
    }
}

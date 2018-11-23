using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;


namespace Jarvis.Framework.CommitBackup.Core
{
    /// <summary>
    /// super simple class to read commit directly from mongo.
    /// </summary>
    public class PlainCommitMongoReader : ICommitReader
    {
        private IMongoCollection<BsonDocument> _commits;

        public PlainCommitMongoReader(IMongoDatabase database)
        {
            _commits = database.GetCollection<BsonDocument>("Commits");  
        }

        public IEnumerable<BsonDocument> GetCommits(long commitStart)
        {
            var reader = _commits.Find(Builders<BsonDocument>.Filter.Gt("_id", commitStart));
            return reader.ToEnumerable();
        }
    }
}

using MongoDB.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Framework.CommitBackup.Core
{
    public interface ICommitReader
    {
        /// <summary>
        /// Retrieve all commits on the store, starting from a specific commit
        /// </summary>
        /// <param name="commitStart"></param>
        /// <returns></returns>
        IEnumerable<BsonDocument> GetCommits(Int64 commitStart);

    }
}

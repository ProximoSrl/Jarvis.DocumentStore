using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Framework.CommitBackup.Core
{
    public interface ICommitWriter : ICommitReader
    {
        void Append(Int64 commitId, BsonDocument commit);

        void Close();

        Int64 GetLastCommitAppended();
    }
}

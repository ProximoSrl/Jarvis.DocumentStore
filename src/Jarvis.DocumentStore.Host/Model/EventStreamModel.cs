using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NEventStore;

namespace Jarvis.DocumentStore.Host.Model
{
    public class CommitModel
    {
        public int Sequence { get; private set; }
        public DateTime Date { get; private set; }
        public Guid CommitId { get; private set; }
        public string CheckPoint { get; private set; }
        
        public IDictionary<string, object> Headers { get; private set; }
        public IList<EventMessage> Events { get; private set; }

        public CommitModel(ICommit commit)
        {
            this.Sequence = commit.CommitSequence;
            this.CheckPoint = commit.CheckpointToken;
            this.Date = commit.CommitStamp;
            this.CommitId = commit.CommitId;
            this.Headers = commit.Headers;
            Events = commit.Events.ToList();
        }
    }
}

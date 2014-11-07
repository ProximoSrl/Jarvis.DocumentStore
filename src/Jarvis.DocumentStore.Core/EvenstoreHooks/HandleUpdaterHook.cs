using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using NEventStore;
using NEventStore.Persistence;

namespace Jarvis.DocumentStore.Core.EvenstoreHooks
{
    public class HandleUpdaterHook : PipelineHookBase
    {
        private readonly IHandleWriter _handleWriter;
        private static readonly string DocumentTypeName = typeof (Document).FullName;

        public HandleUpdaterHook(IHandleWriter handleWriter)
        {
            _handleWriter = handleWriter;
        }

        public override void PostCommit(ICommit committed)
        {
            if (!committed.Headers.ContainsKey("AggregateType"))
                return;

            var type = (string)committed.Headers["AggregateType"];
            if (type != DocumentTypeName)
                return;

            var docCreated = committed.Events.Where(x => x.Body is DocumentCreated).Select(x => (DocumentCreated)x.Body).FirstOrDefault();
            if (docCreated != null)
            {
                _handleWriter.Promise(
                    docCreated.HandleInfo.Handle,
                    docCreated.HandleInfo.FileName,
                    (DocumentId)docCreated.AggregateId,
                    LongCheckpoint.Parse(committed.CheckpointToken).LongValue
                );
            }
        }
    }
}

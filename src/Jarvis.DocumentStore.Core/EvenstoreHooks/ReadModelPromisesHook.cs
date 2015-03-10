using System.Linq;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using NEventStore;

namespace Jarvis.DocumentStore.Core.EvenstoreHooks
{
    public class ReadModelPromisesHook : PipelineHookBase
    {
        private readonly IDocumentWriter _handleWriter;
        private static readonly string DocumentTypeName = typeof (DocumentDescriptor).FullName;

        public ReadModelPromisesHook(IDocumentWriter handleWriter)
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

            HandleDocumentCreation(committed);
        }

        void HandleDocumentCreation(ICommit committed)
        {
            var docCreated = committed.Events
                .Where(x => x.Body is DocumentDescriptorInitialized)
                .Select(x => (DocumentDescriptorInitialized) x.Body)
                .FirstOrDefault();

            if (docCreated != null)
            {
                _handleWriter.Promise(
                    docCreated.HandleInfo.Handle,
                    LongCheckpoint.Parse(committed.CheckpointToken).LongValue
                );
            }
        }
    }
}

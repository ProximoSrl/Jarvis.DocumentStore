using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Kernel.ProjectionEngine.RecycleBin;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using NEventStore;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class CleanupJob : ITenantJob
    {
        IRecycleBin RecycleBin { get; set; }
        public ILogger Logger { get; set; }
        IFileStore FileStore { get; set; }
        public IStoreEvents Store { get; set; }

        public CleanupJob(IRecycleBin recycleBin, IFileStore fileStore)
        {
            RecycleBin = recycleBin;
            FileStore = fileStore;
        }

        public void Execute(IJobExecutionContext context)
        {
            var list = RecycleBin.Slots
                .Where(x => x.Id.StreamId.StartsWith("Document_"))
                .Take(200)
                .ToArray();

            foreach (var slot in list)
            {
                Logger.DebugFormat("Deleting slot {0}", slot.Id.StreamId);
                var fileIds = (FileId[])slot.Data["files"];
                foreach (var fileId in fileIds)
                {
                    Logger.DebugFormat("....deleting file {0}", fileId);
                    FileStore.Delete(fileId);
                }

                RecycleBin.Purge(slot.Id);

                Logger.DebugFormat("....deleting stream {0}.{1}", slot.Id.BucketId, slot.Id.StreamId);
                Store.Advanced.DeleteStream(slot.Id.BucketId, slot.Id.StreamId);
                Logger.DebugFormat("Slot {0} deleted", slot.Id.StreamId);
            }
        }

        public TenantId TenantId { get; set; }
    }
}

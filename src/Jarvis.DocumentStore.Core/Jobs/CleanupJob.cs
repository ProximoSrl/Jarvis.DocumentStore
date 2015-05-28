using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.Framework.Shared.MultitenantSupport;
using NEventStore;
using Quartz;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs
{
    [DisallowConcurrentExecution]
    public class CleanupJob : ITenantJob
    {
        IRecycleBin RecycleBin { get; set; }
        public ILogger Logger { get; set; }
        IBlobStore BlobStore { get; set; }
        public IStoreEvents Store { get; set; }

        public CleanupJob(IRecycleBin recycleBin, IBlobStore blobStore)
        {
            RecycleBin = recycleBin;
            BlobStore = blobStore;
            Logger = NullLogger.Instance;
        }

        public void Execute(IJobExecutionContext context)
        {
            Logger.DebugFormat("Running cleanup on {0} ", 
                context.JobDetail.JobDataMap.GetString(JobKeys.TenantId), 
                RecycleBin);
            DateTime checkDate = DateTimeService.UtcNow.AddDays(-15);
            var list = RecycleBin.Slots
                .Where(x => x.DeletedAt < checkDate &&
                            x.Id.StreamId.StartsWith("DocumentDescriptor_"))
                .Take(200)
                .ToArray();

            foreach (var slot in list)
            {
                Logger.DebugFormat("Deleting slot {0}", slot.Id.StreamId);
                var blobIds = (BlobId[])slot.Data["files"];
                foreach (var blobId in blobIds)
                {
                    Logger.DebugFormat("....deleting file {0}", blobId);
                    BlobStore.Delete(blobId);
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

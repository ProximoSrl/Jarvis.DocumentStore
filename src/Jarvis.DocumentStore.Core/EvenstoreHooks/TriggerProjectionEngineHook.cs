using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.MultitenantSupport;
using NEventStore;

namespace Jarvis.DocumentStore.Core.EvenstoreHooks
{
    public class TriggerProjectionEngineHook : PipelineHookBase
    {
        public ILogger Logger { get; set; }
        public ITenantAccessor TenantAccessor { get; set; }
        public ITriggerProjectionsUpdate Updater { get; set; }

        public TriggerProjectionEngineHook()
        {
        }

        public override void PostCommit(ICommit committed)
        {
            Logger.DebugFormat(
                "New commit {0} on tenant {1}",
                committed.CheckpointToken,
                TenantAccessor.Current.Id
            );
            Updater.Update();
        }
    }
}

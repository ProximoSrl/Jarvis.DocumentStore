using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.MultitenantSupport;
using NEventStore;

namespace Jarvis.DocumentStore.Core.EvenstoreHooks
{
    public class TriggerProjectionEngineHook : PipelineHookBase
    {
        public ILogger Logger { get; set; }
        public ITenantAccessor TenantAccessor { get;private  set; }
        public ITriggerProjectionsUpdate Updater { get; private set; }

        public TriggerProjectionEngineHook(
            ITriggerProjectionsUpdate updater,
            ITenantAccessor tenantAccessor)
        {
            Updater = updater;
            TenantAccessor = tenantAccessor;
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

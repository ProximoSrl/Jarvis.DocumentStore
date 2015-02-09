using Jarvis.Framework.Shared.MultitenantSupport;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public interface ITenantJob : IJob
    {
        TenantId TenantId { get; set; }
    }
}
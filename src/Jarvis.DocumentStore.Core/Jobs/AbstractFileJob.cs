using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public abstract class AbstractFileJob : IJob
    {
        public abstract void Execute(IJobExecutionContext context);
    }
}
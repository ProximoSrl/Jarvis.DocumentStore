using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public abstract class AbstractFileJob : IJob
    {
        public abstract void Execute(IJobExecutionContext context);
    }
}
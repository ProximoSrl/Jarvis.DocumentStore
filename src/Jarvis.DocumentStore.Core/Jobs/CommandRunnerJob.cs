using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Castle.Core.Logging;
using CQRS.Kernel.Commands;
using CQRS.Kernel.Store;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Processing;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class CommandRunnerJob<T> : IJob where T:ICommand
    {
        public ILogger Logger { get; set; }
        public ICommandHandler<T>  CommandHandler { get; set; }
        public void Execute(IJobExecutionContext context)
        {
            var cmdString = context.JobDetail.JobDataMap.GetString(JobKeys.Command);
            Logger.DebugFormat("Executing {0}", cmdString);
            var command = CommandSerializer.Deserialize<T>(cmdString);
            CommandHandler.Handle(command);
            Logger.DebugFormat("Executed {0}", cmdString);
        }
    }
}

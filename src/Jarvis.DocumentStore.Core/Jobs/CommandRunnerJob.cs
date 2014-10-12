using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Castle.Core.Logging;
using CQRS.Kernel.Commands;
using CQRS.Kernel.Engine;
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
            int i = 0;
            bool done = false;

            while (!done && i < 100)
            {
                try
                {
                    CommandHandler.Handle(command);
                    done = true;
                }
                catch (ConflictingCommandException ex)
                {
                    // retry
                    Logger.DebugFormat("Handled {0} {1}, concurrency exception. Retrying",
                        command.GetType().FullName, command.MessageId);
                    if (i++ > 5)
                    {
                        Thread.Sleep(new Random(DateTime.Now.Millisecond).Next(i * 10));
                    }
                }
                catch (DomainException ex)
                {
                    Logger.ErrorFormat(ex, "Failed command {0}", command.MessageId);
                    done = true;
                }
            }
            Logger.DebugFormat("Executed {0}", cmdString);
        }
    }
}

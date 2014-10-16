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
            var command = CommandSerializer.Deserialize<T>(cmdString);
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("Executing command {0}\n{1}", 
                    command.MessageId,
                    cmdString
                );
            }

            int i = 0;
            bool done = false;

            while (!done && i < 100)
            {
                try
                {
                    CommandHandler.Handle(command);
                    done = true;
                    Logger.DebugFormat("Executed command {0}", command.MessageId);
                }
                catch (ConflictingCommandException ex)
                {
                    // retry
                    Logger.WarnFormat("Handled {0} {1}, concurrency exception. Retrying",
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
        }
    }
}

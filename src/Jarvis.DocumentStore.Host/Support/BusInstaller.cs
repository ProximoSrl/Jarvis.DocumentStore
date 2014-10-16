using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Commands;
using CQRS.Shared.Commands;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Host.Commands;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Support
{
    public class BusInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var logUrl = new MongoUrl(ConfigurationManager.ConnectionStrings["log"].ConnectionString);
            var logDb = new MongoClient(logUrl).GetServer().GetDatabase(logUrl.DatabaseName);
            
            container.Register(
                Component
                    .For<JobHandlersRegistration>()
                    .DependsOn(Dependency.OnValue<IWindsorContainer>(container))
                    .DependsOn(Dependency.OnValue<Assembly[]>(new[]
                        {
                            typeof (Document).Assembly,
                        }))
                    .StartUsingMethod(x => x.Register),
                Classes
                    .FromAssemblyContaining<Document>()
                    .BasedOn(typeof(ICommandHandler<>))
                    .WithServiceFirstInterface()
                    .LifestyleTransient(),
                Component
                    .For<ICommandBus>()
                    .ImplementedBy<DocumentStoreCommandBus>(),
                Component
                    .For<IMessagesTracker>()
                    .ImplementedBy<MongoDbMessagesTracker>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(logDb))
                );
        }
    }

    public class DocumentStoreCommandBus : ICommandBus
    {
        private readonly IJobHelper _jobHelper;

        public DocumentStoreCommandBus(IJobHelper jobHelper)
        {
            _jobHelper = jobHelper;
        }

        public ICommand Send(ICommand command, string impersonatingUser = null)
        {
            _jobHelper.QueueCommand(command, impersonatingUser);
            return command;
        }

        public ICommand Defer(TimeSpan delay, ICommand command, string impersonatingUser = null)
        {
            throw new NotImplementedException();
        }

        public ICommand SendLocal(ICommand command, string impersonatingUser = null)
        {
            throw new NotImplementedException();
        }
    }
}

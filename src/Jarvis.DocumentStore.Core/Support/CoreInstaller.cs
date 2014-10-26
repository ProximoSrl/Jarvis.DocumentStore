using System;
using System.Configuration;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
using CQRS.Shared.Factories;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.Stats;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CoreTenantInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<IFileStore>()
                    .ImplementedBy<GridFSFileStore>(),
                Component
                    .For<IPipelineManager>()
                    .ImplementedBy<PipelineManager>(),
                Component
                    .For<GridFsFileStoreStats>(),
                Component
                    .For<MongoGridFS>()
                    .UsingFactoryMethod(k=> k.Resolve<ITenant>().Get<MongoGridFS>("grid.fs"))
            );
        }
    }

    public class CoreInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<ICommandBus>()
                    .ImplementedBy<DocumentStoreCommandBus>(),
                Component
                    .For<IInProcessCommandBus>()
                    .ImplementedBy<MultiTenantInProcessCommandBus>(),
                Component
                    .For<ConfigService>()
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

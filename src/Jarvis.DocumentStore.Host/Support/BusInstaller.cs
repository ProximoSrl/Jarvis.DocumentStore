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
using CQRS.Bus.Rebus.Integration.Adapters;
using CQRS.Bus.Rebus.Integration.Support;
using CQRS.Kernel.Commands;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.CommandHandlers;

namespace Jarvis.DocumentStore.Host.Support
{
    public class BusInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["filestore"].ConnectionString;
            
            container.Register(
                Component
                    .For<CommandHandlersRegistration>()
                    .DependsOn(Dependency.OnValue<IWindsorContainer>(container))
                    .DependsOn(Dependency.OnValue<Assembly[]>(new[]
                        {
                            typeof (CreateDocumentCommandHandler).Assembly,
                        }))
                    .StartUsingMethod(x => x.Register),
                Classes
                    .FromAssemblyContaining<CreateDocumentCommandHandler>()
                    .BasedOn(typeof(ICommandHandler<>))
                    .WithServiceFirstInterface()
                    .LifestyleTransient(),
                Component
                    .For<BusBootstrapper>()
                    .DependsOn(Dependency.OnValue<IWindsorContainer>(container))
                    .DependsOn(Dependency.OnValue("connectionString",connectionString))
                    .DependsOn(Dependency.OnValue("prefix", "ds"))
                    .StartUsingMethod(x => x.Start),
                Component
                    .For<ICommandBus>()
                    .ImplementedBy<RebusCommandBus>()
                );
        }
    }
}

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
using Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Host.Commands;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Support
{
    public class TenantHandlersInstaller : IWindsorInstaller
    {
        readonly ITenant _tenant;

        public TenantHandlersInstaller(ITenant tenant)
        {
            _tenant = tenant;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var sysDb = _tenant.Get<MongoDatabase>("system.db");
            
            container.Register(
                Component
                    .For<JobHandlersRegistration>()
                    .DependsOn(Dependency.OnValue<IWindsorContainer>(container))
                    .DependsOn(Dependency.OnValue<Assembly[]>(new[]
                        {
                            typeof (DocumentDescriptor).Assembly,
                        }))
                    .StartUsingMethod(x => x.Register),
                Component
                    .For<IHandleMapper>()
                    .ImplementedBy<HandleMapper>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(sysDb)),
                Classes
                    .FromAssemblyContaining<DocumentDescriptor>()
                    .BasedOn(typeof(ICommandHandler<>))
                    .WithServiceFirstInterface()
                    .LifestyleTransient()
                );
        }
    }
}

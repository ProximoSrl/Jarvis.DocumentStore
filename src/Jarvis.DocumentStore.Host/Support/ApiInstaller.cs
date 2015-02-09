using System.Web.Http;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Host.Controllers;

namespace Jarvis.DocumentStore.Host.Support
{
    public class ApiInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Classes
                    .FromThisAssembly()
                    .BasedOn<ApiController>()
                    .Unless(type => typeof(ITenantController).IsAssignableFrom(type))
                    .LifestyleTransient()
                );
        }
    }

    public class TenantApiInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Classes
                    .FromThisAssembly()
                    .BasedOn<ITenantController>()
                    .WithServiceSelf()
                    .LifestyleTransient()
                );
        }
    }
}
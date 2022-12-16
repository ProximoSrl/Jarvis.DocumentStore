using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<OfficeToPdfConverterOptions>()
                    .LifeStyle.Singleton,
                Component
                    .For<MsGraphOfficePdfThumbnail>()
                    .LifeStyle.Singleton,
                Component
                    .For<AuthenticationService>()
                    .LifeStyle.Singleton
           );
        }
    }
}

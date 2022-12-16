using Castle.MicroKernel.Registration;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficeToPdf
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
                    .For<MsGraphOfficeToPdfConverter>()
                    .LifeStyle.Singleton,
                Component
                    .For<AuthenticationService>()
                    .LifeStyle.Singleton
           );
        }
    }
}

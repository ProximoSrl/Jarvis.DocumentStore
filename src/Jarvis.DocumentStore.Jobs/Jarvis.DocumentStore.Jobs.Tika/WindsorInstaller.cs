using Castle.MicroKernel.Registration;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(
                Component.For<ContentFilterManager>(),
                Component.For<ContentFormatBuilder>()
            );
        }
    }
}

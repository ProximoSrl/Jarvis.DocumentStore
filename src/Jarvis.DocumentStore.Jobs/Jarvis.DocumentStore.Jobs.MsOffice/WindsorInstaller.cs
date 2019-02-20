using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.Jobs.MsOffice;

namespace Jarvis.DocumentStore.Jobs.LibreOffice
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<WordConverter>()
                    .ImplementedBy<WordConverter>()
                    .LifeStyle.Singleton,
                Component
                    .For<PowerPointConverter>()
                    .ImplementedBy<PowerPointConverter>()
                    .LifeStyle.Singleton,
                Component
                    .For<ExcelConverter>()
                    .ImplementedBy<ExcelConverter>()
                    .LifeStyle.Singleton
           );
        }
    }
}

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
                    .LifeStyle.Transient,
                Component
                    .For<PowerPointConverter>()
                    .ImplementedBy<PowerPointConverter>()
                    .LifeStyle.Transient,
                Component
                    .For<ExcelConverter>()
                    .ImplementedBy<ExcelConverter>()
                    .LifeStyle.Transient
           );
        }
    }
}

using Castle.MicroKernel.Registration;
using System.Configuration;

namespace Jarvis.DocumentStore.Jobs.LibreOffice
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            var useExe = ConfigurationManager.AppSettings["use-direct-exe-conversion"];
            if ("true".Equals(useExe, System.StringComparison.OrdinalIgnoreCase))
            {
                container.Register(
                Component
                    .For<ILibreOfficeConversion>()
                    .ImplementedBy<LibreOfficeConversion>()
                    .LifeStyle.Transient);
            }
            else
            {
                container.Register(
                Component
                    .For<ILibreOfficeConversion>()
                    .ImplementedBy<LibreOfficeUnoConversion>()
                    .LifeStyle.Transient);
            }
        }
    }
}

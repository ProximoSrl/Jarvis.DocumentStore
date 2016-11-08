using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.Jobs.PdfConverter.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.PdfConverter
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(
                   Classes.FromThisAssembly()
                   .BasedOn<IPdfConverter>()
                   .WithServiceFirstInterface()
            );
        }

    }
}

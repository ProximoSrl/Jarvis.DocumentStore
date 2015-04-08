using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.Jobs.Tika;
using Jarvis.DocumentStore.Jobs.Tika.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

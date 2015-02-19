using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.JobsHost.Processing.Pdf;

namespace Jarvis.DocumentStore.Jobs.PdfThumbnails
{
    class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<CreateImageFromPdfTask>()
                    .LifestyleTransient());
        }
    }
}

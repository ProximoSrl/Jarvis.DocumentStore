using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;

namespace Jarvis.DocumentStore.Jobs.PdfThumbnails
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
        {
            container.Register(
                  Component
                    .For<PdfDecrypt>(),
                 Component
                    .For<CreateImageFromPdfTask>()
                    .ImplementedBy<CreateImageFromPdfTask>()
                    .LifestyleTransient(),
                  Component
                     .For<Func<CreateImageFromPdfTask>>()
                     .Instance(() => container.Resolve<CreateImageFromPdfTask>())
                     );
            //var check = container.Resolve<CreateImageFromPdfTask>();
        }
    }
}

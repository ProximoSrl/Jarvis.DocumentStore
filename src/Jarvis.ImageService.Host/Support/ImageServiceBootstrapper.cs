using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Microsoft.Owin.Hosting;

namespace Jarvis.ImageService.Host.Support
{
    public class ImageServiceBootstrapper
    {
        IDisposable _webApplication;
        readonly Uri _serverAddress;
        IWindsorContainer _container;

        public ImageServiceBootstrapper(Uri serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public void Start()
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.AddFacility<LoggingFacility>(f => f.UseLog4Net("log4net"));
            _container.Install(new ApiInstaller());

            _webApplication = WebApp.Start<ImageServiceApplication>(_serverAddress.AbsoluteUri);
        }

        public void Stop()
        {
            _webApplication.Dispose();
            _container.Dispose();
        }
    }

    public class ApiInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Classes
                    .FromThisAssembly()
                    .BasedOn<ApiController>()
                    .LifestyleTransient()
            );
        }
    }
}

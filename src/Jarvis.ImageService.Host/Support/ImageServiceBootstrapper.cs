using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Windsor;
using Jarvis.ImageService.Core.Support;
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
            _container.AddFacility<StartableFacility>();

            var connectionString = ConfigurationManager.ConnectionStrings["filestore"].ConnectionString;
            _container.Install(
                new ApiInstaller(), 
                new CoreInstaller(connectionString),
                new SchedulerInstaller(connectionString)
            );

            _container.Resolve<ILogger>().InfoFormat("Started server @ {0}", _serverAddress.AbsoluteUri);
            _webApplication = WebApp.Start<ImageServiceApplication>(_serverAddress.AbsoluteUri);
        }

        public void Stop()
        {
            _webApplication.Dispose();
            _container.Dispose();
        }
    }
}

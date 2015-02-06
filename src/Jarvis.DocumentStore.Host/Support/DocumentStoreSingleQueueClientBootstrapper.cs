using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Services.Logging.Log4netIntegration;
using Castle.Windsor;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Messages;
using CQRS.Shared.MultitenantSupport;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Support;
using Microsoft.Owin.Hosting;
using Rebus.Logging;
using Jarvis.DocumentStore.Core.Services;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreSingleQueueClientBootstrapper
    {
        IDisposable _webApplication;
        readonly Uri _serverAddress;
        readonly String _handle;
        readonly String _queueName;
        IWindsorContainer _container;
        ILogger _logger;

        public DocumentStoreSingleQueueClientBootstrapper(Uri serverAddress, String queueName, String handle)
        {
            _serverAddress = serverAddress;
            _queueName = queueName;
            _handle = handle;
        }

        public Boolean Start(DocumentStoreConfiguration config)
        {
            Console.WriteLine("Starting");
            BuildContainer(config);

            var allPollers = _container.ResolveAll<IPollerJob>();
            foreach (var poller in allPollers)
            {
                _logger.InfoFormat("Poller: {0} - IsOutOfProcess {1} - IsActive {2} - Type {3}", poller.QueueName, poller.IsOutOfProcess, poller.IsActive, poller.GetType().Name);
            }
            var queuePoller = allPollers.SingleOrDefault(p =>
                p.IsOutOfProcess && 
                p.QueueName == _queueName &&
                p.IsActive);
          
            if (queuePoller == null)
            {
                _logger.ErrorFormat("No configured poller for queue {0}", _queueName);
                return false;
            }
            else
            {
                _logger.InfoFormat("Start poller for queue {0} implemented by {1}", _queueName, queuePoller.GetType().Name);
                queuePoller.Start(new List<String>() { _serverAddress.AbsoluteUri}, _handle);
            }
            return true;
        }

        void BuildContainer(DocumentStoreConfiguration config)
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.Register(Component.For<DocumentStoreConfiguration>().Instance(config));
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel, true));
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel, true));


            _container.AddFacility<LoggingFacility>(config.CreateLoggingFacility);

            _container.AddFacility<StartableFacility>();
            _container.AddFacility<TypedFactoryFacility>();

            _container.Register(
                Component
                    .For<ConfigService>(),
                Component
                    .For<ILibreOfficeConversion>()
                    .ImplementedBy<LibreOfficeUnoConversion>()
                    .LifeStyle.Transient,
                Classes.FromAssemblyInThisApplication()
                    .BasedOn<IPollerJob>()
                    .WithServiceFirstInterface()
            );
            _logger = _container.Resolve<ILoggerFactory>().Create(GetType());

        }

      

        public void Stop()
        {
           

            _container.Dispose();
        }
    }

  
}

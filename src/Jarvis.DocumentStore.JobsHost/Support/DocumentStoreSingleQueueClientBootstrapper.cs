using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.JobsHost.Support
{
    public class DocumentStoreSingleQueueClientBootstrapper
    {
        IDisposable _webApplication;
        readonly Uri _serverAddress;
        readonly String _handle;
        readonly String _queueName;
        IWindsorContainer _container;
        ILogger _logger;
        JobsHostConfiguration _config;

        public DocumentStoreSingleQueueClientBootstrapper(Uri serverAddress, String queueName, String handle)
        {
            _serverAddress = serverAddress;
            _queueName = queueName;
            _handle = handle;
        }

        public Boolean Start(JobsHostConfiguration config)
        {
            Console.WriteLine("Starting");
            _config = config;
            BuildContainer(config);

            var allPollers = _container.ResolveAll<IPollerJob>();
            foreach (var poller in allPollers)
            {
                _logger.InfoFormat("Poller: {0} - IsOutOfProcess {1} - IsActive {2} - Type {3}", poller.QueueName, poller.IsOutOfProcess, poller.IsActive, poller.GetType().Name);
            }

            var testResult = ExecuteTests();
            if (!testResult) return false;

            var queuePoller = allPollers.SingleOrDefault(p =>
                p.IsOutOfProcess && 
                p.QueueName.Equals(_queueName, StringComparison.OrdinalIgnoreCase) &&
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

        private Boolean ExecuteTests()
        {
            var tests = _container.ResolveAll<IPollerTest>();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Executing Startup Tests");
            Boolean testFailed = false;
            foreach (var test in tests)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Executing:" + test.Name);
                var results = test.Execute();
                foreach (var result in results)
                {
                    if (result.Result)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("PASS: ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("FAIL: ");
                        testFailed = true;
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(result.Message);
                }
            }
            return !testFailed;
        }

        void BuildContainer(JobsHostConfiguration config)
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.Register(Component.For<JobsHostConfiguration>().Instance(config));
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel, true));
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel, true));

            _container.AddFacility<LoggingFacility>(config.CreateLoggingFacility);
            _container.AddFacility<StartableFacility>();
            _container.AddFacility<TypedFactoryFacility>();

            _container.Register(
                Component.For<IClientPasswordSet>()
                    .ImplementedBy<EnvironmentVariableClientPasswordSet>(),

                //Register from this application
                Classes.FromAssemblyInThisApplication()
                    .BasedOn<IPollerJob>()
                    .WithServiceFirstInterface() ,
                Classes.FromAssemblyInThisApplication()
                    .BasedOn<IPollerTest>()
                    .WithServiceFirstInterface() ,

                //Register from dll that contains Jobs in name.
                Classes.FromAssemblyInDirectory(new AssemblyFilter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.Jobs.*.*")) 
                    .BasedOn<IPollerJob>()
                    .WithServiceFirstInterface(),
                  Classes.FromAssemblyInDirectory(new AssemblyFilter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.Jobs.*.*"))
                    .BasedOn<IPollerTest>()
                    .WithServiceFirstInterface()
            );

            _container.Install(
                FromAssembly.InDirectory(
                    new AssemblyFilter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.Jobs.*.*")));

            _logger = _container.Resolve<ILogger>();
        }
    }

   

}

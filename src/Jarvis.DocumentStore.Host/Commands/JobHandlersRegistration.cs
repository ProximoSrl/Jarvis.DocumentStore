using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Host.Commands
{
    public class JobHandlersRegistration
    {
        private ILogger _logger = NullLogger.Instance;
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        private readonly IWindsorContainer _container;
        private readonly Assembly[] _assemblies;

        public JobHandlersRegistration(IWindsorContainer container, params Assembly[] assemblies)
        {
            this._container = container;
            this._assemblies = assemblies;
        }

        /// <summary>
        /// Permette la registrazione dei command handlers presenti negli assembly indicati
        /// </summary>
        public void Register()
        {
            foreach (var assembly in _assemblies)
            {
                RegisterAssembly(assembly);
            }
        }

        /// <summary>
        /// Registra i command handler presenti nell'assembly indicato
        /// </summary>
        /// <param name="assembly">assembly in cui ricercare i command handler</param>
        private void RegisterAssembly(Assembly assembly)
        {
            Logger.DebugFormat("Assembly registration {0}", assembly.FullName);
            var types = assembly.GetTypes().Where(x => typeof(ICommandHandler<>).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract).ToList();

            foreach (var type in types)
            {
                var commandHandlerServiceType = type.GetInterfaces().Single(x =>
                    x.IsGenericType &&
                    x.GetGenericTypeDefinition() ==
                    typeof(ICommandHandler<>)
                );

                var commandType = commandHandlerServiceType.GetGenericArguments()[0];
                var messageHandlerType = typeof(CommandRunnerJob<>).MakeGenericType(commandType);

                Logger.DebugFormat("\t{0} -> {1}", commandType, messageHandlerType.FullName);

                _container.Register(
                    Component
                        .For(messageHandlerType)
                        .LifestyleTransient()
                );
            }

            Logger.Debug("Registration completed");
        }
    }
}

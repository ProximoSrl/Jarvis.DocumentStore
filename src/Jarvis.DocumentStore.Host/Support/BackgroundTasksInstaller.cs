using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.BackgroundTasks;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Host.Support
{
    public class BackgroundTasksInstaller : IWindsorInstaller
    {
        private readonly DocumentStoreConfiguration _config;

        public BackgroundTasksInstaller(DocumentStoreConfiguration config)
        {
            _config = config;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            if (_config.EnableImportFormFileSystem)
            {
                container.Register(
                    Component
                        .For<ImportFormatFromFileQueue>()
                        .DependsOn(Dependency.OnValue<string[]>(_config.FoldersToMonitor)),
                    Component
                        .For<ImportFileFromFileSystemRunner>()
                        .Start()
                );
            }
        }
    }
}

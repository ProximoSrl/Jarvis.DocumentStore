using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Jarvis.DocumentStore.LiveBackup.Support
{
    public static class ServiceInstallHelper
    {
        public static TopshelfExitCode StartForInstallOrUninstall(
                Boolean runAsSystem,
                String dependOnServiceList,
                String serviceDescriptiveName,
                String serviceName
            )
        {
            var exitCode = HostFactory.Run(host =>
            {
                host.Service<Object>(service =>
                {
                    service.ConstructUsing(() => new Object());
                    service.WhenStarted(s => Console.WriteLine("Start fake for install"));
                    service.WhenStopped(s => Console.WriteLine("Stop fake for install"));
                });
                if (runAsSystem)
                {
                    host.RunAsLocalSystem();
                }
                else
                {
                    host.RunAsNetworkService();
                }
                host.DependsOnMsmq();

                foreach (var dependency in dependOnServiceList.Split(',')
                     .Select(d => d.Trim())
                     .Where(d => !string.IsNullOrWhiteSpace(d)))
                {
                    host.DependsOn(dependency);
                }

                host.SetDescription(serviceDescriptiveName);
                host.SetDisplayName(serviceDescriptiveName);
                host.SetServiceName(serviceName);
            });
            return exitCode;
        }
    }
}

using Castle.Core.Logging;
using Castle.MicroKernel;
using Castle.Windsor;
using Castle.Windsor.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Support
{
    public static class WindsorContainerUtils
    {
        public static void CheckConfiguration(this IWindsorContainer container)
        {
            var logger = container.Resolve<ILogger>();
            var host = (IDiagnosticsHost)container.Kernel.GetSubSystem(SubSystemConstants.DiagnosticsKey);
            var diagnostics = host.GetDiagnostic<IPotentiallyMisconfiguredComponentsDiagnostic>();

            //Exclude CommitPollingClient, it seems misconfigured because a reference is passed through factory.
            var misconfiguredHandlers = diagnostics.Inspect()
                 .Where(h => !h.ComponentModel.ComponentName.Name.Contains("CommitPollingClient"));

            foreach (var handler in misconfiguredHandlers)
            {
                logger.ErrorFormat("Misconfigured: {0}", handler.ComponentModel.ComponentName);
                try
                {
                    container.Resolve(handler.ComponentModel.Name, handler.ComponentModel.Services.First());
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex, "Misconfigured container: component {0}", handler.ComponentModel.Services.First());
                }
            }

            if (misconfiguredHandlers.Any())
            {
                throw new Exception("Container misconfigured!");
            }
        }
    }
}

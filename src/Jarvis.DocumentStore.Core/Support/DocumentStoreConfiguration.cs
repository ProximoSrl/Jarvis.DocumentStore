using System.Collections.Generic;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using CQRS.Kernel.MultitenantSupport;

namespace Jarvis.DocumentStore.Core.Support
{
    public abstract class DocumentStoreConfiguration
    {
        protected DocumentStoreConfiguration()
        {
            TenantSettings = new List<TenantSettings>();
        }

        public bool IsApiServer { get; protected set; }
        public bool IsWorker { get; protected set; }
        public bool IsReadmodelBuilder { get; protected set; }
        public bool IsQueueManager { get; protected set; }
        public string QuartzConnectionString { get; protected set; }
        public string QueueConnectionString { get; protected set; }
        public IList<TenantSettings> TenantSettings { get; private set; }

        public virtual void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing(new ExtendedLog4netFactory("log4net"));
        }
    }
}
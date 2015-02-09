using System.Collections.Generic;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.Framework.Kernel.MultitenantSupport;


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

        public System.Uri ServerAddress { get; set; }

        public JobModes JobMode { get; protected set; }
        
        public bool IsReadmodelBuilder { get; protected set; }
        public bool IsQueueManager { get; protected set; }
        public QueueInfo[] QueueInfoList { get; protected set; }
        public string QuartzConnectionString { get; protected set; }
        public string QueueConnectionString { get; protected set; }
        public int QueueStreamPollInterval { get; protected set; }

        public int QueueJobsPollInterval { get; protected set; }

        public IList<TenantSettings> TenantSettings { get; private set; }

        public virtual void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing(new ExtendedLog4netFactory("log4net.config"));
        }
    }

    public enum JobModes 
    {
        Unknown = 0,
        Quartz = 1,
        Queue = 2,
    }
}
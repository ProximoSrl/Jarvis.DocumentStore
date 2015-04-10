using System;
using System.Collections.Generic;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.Framework.Kernel.MultitenantSupport;
using System.Collections;
using System.Linq;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Newtonsoft.Json;
using Jarvis.DocumentStore.Core.Model;


namespace Jarvis.DocumentStore.Core.Support
{
    public abstract class DocumentStoreConfiguration
    {
        protected DocumentStoreConfiguration()
        {
            TenantSettings = new List<TenantSettings>();
        }

        #region BasicSettings

        public bool IsApiServer { get; protected set; }
        public bool IsWorker { get; protected set; }
        
        public bool HasMetersEnabled {
            get { return MetersOptions.Any(); }
        }
        private readonly IList<Uri> _addresses = new List<Uri>();
        public readonly IDictionary<string,string> MetersOptions = new Dictionary<string, string>();

        public Uri[] ServerAddresses
        {
            get { return _addresses.ToArray(); }
        }

        public bool IsDeduplicationActive { get; protected set; }

        public String[] AllowedFileTypes { get; protected set; }

         public bool IsFileAllowed(FileNameWithExtension filename)
        {
            if (AllowedFileTypes == null)
                return true;

            return AllowedFileTypes.Contains(filename.Extension);
        }

        public String LogsConnectionString { get; set; }

        #endregion

        #region Queue Management

        public bool IsReadmodelBuilder { get; protected set; }
        public bool IsQueueManager { get; protected set; }
        public QueueInfo[] QueueInfoList { get; protected set; }

        public string QueueConnectionString { get; protected set; }
        public int QueueStreamPollInterval { get; protected set; }

        public int QueueJobsPollInterval { get; protected set; }

        #endregion

        #region Projections

        public Boolean Rebuild { get; set; }

        public String[] EngineSlots { get; set; }

        public Boolean NitroMode { get; set; }

        public Int32 PollingMsInterval { get; set; }

        public Int32 ForcedGcSecondsInterval { get; set; }

        public Int32 DelayedStartInMilliseconds { get; set; }

        public String Boost { get; set; }

        #endregion

        public IList<TenantSettings> TenantSettings { get; private set; }

        public virtual void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing(new ExtendedLog4netFactory("log4net.config"));
        }

        public JobsManagementConfiguration JobsManagement { get; set; }

        protected Uri Expand(Uri address)
        {
            if (address.Host == "machine_name")
            {
                var builder = new UriBuilder(address) { Host = Environment.MachineName };
                address = builder.Uri;
            }

            return address;
        }

        protected void AddServerAddress(Uri address)
        {
            _addresses.Add(Expand(address));
        }

        protected void AddMetersOptions(string name, string value)
        {
            if (value.Contains("machine_name"))
                value = value.Replace("machine_name", Environment.MachineName);
            
            MetersOptions.Add(name, value);
        }

        public static void ParseQueueList(List<QueueInfo> queueInfoList, dynamic queueList)
        {
            foreach (dynamic queue in (IEnumerable)queueList)
            {
                QueueInfo info = JsonConvert.DeserializeObject<QueueInfo>(queue.ToString());
                queueInfoList.Add(info);
            }
        }
    }

    public class JobsManagementConfiguration
    {
        public Boolean WindowVisible { get; set; }
    }

}
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
using Jarvis.Framework.Kernel.ProjectionEngine;

namespace Jarvis.DocumentStore.Core.Support
{
    public abstract class DocumentStoreConfiguration
    {
        protected DocumentStoreConfiguration()
        {
            TenantSettings = new List<TenantSettings>();
            EngineVersion = "v3";
        }

        #region BasicSettings

        public bool IsApiServer { get; protected set; }
        public bool IsWorker { get; protected set; }
        
        public bool HasMetersEnabled {
            get { return MetersOptions.Any(); }
        }
        private readonly IList<String> _addresses = new List<String>();
        public readonly IDictionary<string,string> MetersOptions = new Dictionary<string, string>();

        public String[] ServerAddresses
        {
            get { return _addresses.ToArray(); }
        }

        public String GetServerAddressForJobs()
        {
            //TODO: Handle multiple document store application
            var serverAddress = ServerAddresses.First();
            if (serverAddress.StartsWith("http://+:"))
            {
                serverAddress = serverAddress.Replace("+", Environment.MachineName);
            }
            return serverAddress;
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

        public String EngineVersion { get; set; }

        public List<BucketInfo> BucketInfo { get; set; }


        public Boolean Rebuild { get; set; }

        public String[] EngineSlots { get; set; }

        public Boolean NitroMode { get; set; }

        public Int32 PollingMsInterval { get; set; }

        public Int32 ForcedGcSecondsInterval { get; set; }

        public Int32 DelayedStartInMilliseconds { get; set; }

        public String Boost { get; set; }

        #endregion

        #region Framework

        public Boolean EnableSingleAggregateRepositoryCache { get; set; }

        public Boolean EnableSnapshotCache { get; set; }

        public Boolean DisableRepositoryLockOnAggregateId { get; set; }


        #endregion

        public IList<TenantSettings> TenantSettings { get; private set; }

        public virtual void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing(new ExtendedLog4netFactory("log4net.config"));
        }

        public JobsManagementConfiguration JobsManagement { get; set; }
        public bool EnableImportFormFileSystem { get; private set; }
        public string[] FoldersToMonitor { get; protected set; }

        public void MonitorFolders(string[] folders)
        {
            FoldersToMonitor = folders;
            EnableImportFormFileSystem = folders.Any();
        }

        protected String Expand(String address)
        {
            if (address.Contains("machine_name"))
            {
                var uri = new Uri(address);
                var builder = new UriBuilder(uri) { Host = Environment.MachineName };
                return builder.Uri.AbsoluteUri;
            }

            return address;
        }

        protected void AddServerAddress(String address)
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
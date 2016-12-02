
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.LiveBackup.Support;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    public class ArtifactSyncJobConfig
    {
        public ArtifactSyncJobConfig(String baseDumpDirectory, Configuration.TenantSettings tenantSetting)
        {
            EvenstoreConnection = tenantSetting.EventStoreConnectionString;
            OriginalBlobConnection = tenantSetting.OriginalBlobConnnectionString;
            Directory = System.IO.Path.Combine(baseDumpDirectory, tenantSetting.TenantId);
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);
        }

        public String EvenstoreConnection { get; private set; }

        public String OriginalBlobConnection { get; private set; }

        public String Directory { get; private set; }
    }
}

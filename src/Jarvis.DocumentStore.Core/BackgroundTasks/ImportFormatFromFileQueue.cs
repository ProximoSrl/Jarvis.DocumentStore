using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Serialization;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.BackgroundTasks
{
    public class FileInQueue
    {
        public Uri Uri { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public DocumentFormat Format { get; private set; }
        public TenantId Tenant { get; private set; }
        public DocumentCustomData CustomData { get; private set; }

        public string PathToTaskFile { get; set; }
    }

    public class ImportFormatFromFileQueue
    {
        public const string JobExtension = "*.dsimport";
        public ILogger Logger { get; set; }

        private readonly string[] _foldersToWatch;
        private readonly ITenantAccessor _tenantAccessor;

        public ImportFormatFromFileQueue(string[] foldersToWatch, ITenantAccessor tenantAccessor)
        {
            _foldersToWatch = foldersToWatch;
            _tenantAccessor = tenantAccessor;
        }

        public void PollFileSystem()
        {
            foreach (var folder in _foldersToWatch)
            {
                var files = Directory.GetFiles(folder, JobExtension, SearchOption.AllDirectories);
                Parallel.ForEach(files, file =>
                {
                    var task = LoadTask(file);
                    if (task != null)
                    {
                        Logger.DebugFormat("Loading /{0}/{1}/{2} - {3}", 
                            task.Tenant,
                            task.Handle,
                            task.Format,
                            task.Uri
                        );

                        UploadFile(task);
                    }
                });
            }
        }

        private void UploadFile(FileInQueue task)
        {
            if (!task.Uri.IsFile)
            {
                Logger.ErrorFormat("Uri is not a file: {0}", task.Uri);
                return;
            }

            var fname = task.Uri.LocalPath;
            if (!File.Exists(fname))
            {
                Logger.ErrorFormat("File missing: {0}", fname);
                return;
            }

            var tenant = _tenantAccessor.GetTenant(task.Tenant);
            if(tenant == NullTenant.Instance)
            {
                Logger.ErrorFormat("Tenant {1} not found for file {0}", fname, tenant);
                return;
            }

            var container = tenant.Container;
            var blobStore = container.Resolve<IBlobStore>();
            blobStore.Upload(task.Format, fname);

        }

        public FileInQueue LoadTask(string pathToFile)
        {
            try
            {
                var asJson = File.ReadAllText(pathToFile)
                    .Replace("%CURRENT_DIR%", Path.GetDirectoryName(pathToFile).Replace("\\", "/"));

                var task = JsonConvert.DeserializeObject<FileInQueue>(asJson, PocoSerializationSettings.Default);
                task.PathToTaskFile = pathToFile;
                return task;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "failed to deserialize {0}", pathToFile);
                return null;
            }
        }
    }
}

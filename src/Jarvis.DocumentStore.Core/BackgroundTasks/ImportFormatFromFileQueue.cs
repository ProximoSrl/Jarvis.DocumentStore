using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Serialization;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.Commands;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.BackgroundTasks
{
    internal class DocumentImportTask
    {
        /* input */
        public Uri Uri { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public DocumentFormat Format { get; private set; }
        public TenantId Tenant { get; private set; }
        public DocumentCustomData CustomData { get; private set; }
        public bool DeleteAfterImport { get; private set; }
        
        /* working */
        public string PathToTaskFile { get; set; }
        public string Result { get; set; }
    }

    public class ImportFormatFromFileQueue
    {
        public const string JobExtension = "*.dsimport";
        public ILogger Logger { get; set; }

        private readonly string[] _foldersToWatch;
        private readonly ITenantAccessor _tenantAccessor;
        private readonly ICommandBus _commandBus;

        public ImportFormatFromFileQueue(
            string[] foldersToWatch,
            ITenantAccessor tenantAccessor,
            ICommandBus commandBus
        )
        {
            _foldersToWatch = foldersToWatch;
            _tenantAccessor = tenantAccessor;
            _commandBus = commandBus;
        }

        public void PollFileSystem()
        {
            foreach (var folder in _foldersToWatch)
            {
                if (!Directory.Exists(folder))
                    continue;

                var files = Directory.GetFiles(folder, JobExtension, SearchOption.AllDirectories);
                Parallel.ForEach(files, file =>
                {
                    var task = LoadTask(file);
                    if (task != null)
                    {
                        if (Logger.IsInfoEnabled)
                        {
                            Logger.InfoFormat("Loading /{0}/{1}/{2} - {3}",
                                task.Tenant,
                                task.Handle,
                                task.Format,
                                task.Uri
                            );
                        }

                        UploadFile(task);
                    }
                });
            }
        }

        private void UploadFile(DocumentImportTask task)
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

            try
            {
                TenantContext.Enter(task.Tenant);

                var blobStore = GetBlobStoreForTenant();
                var identityGenerator = GetIdentityGeneratorForTenant();
                if (blobStore == null || identityGenerator == null)
                {
                    Logger.ErrorFormat("Tenant {1} not found or not configured for file: {1}", task.Tenant, fname);
                    return;
                }

                var blobId = blobStore.Upload(task.Format, fname);
                var descriptor = blobStore.GetDescriptor(blobId);
                var fileName = new FileNameWithExtension(Path.GetFileName(fname));
                var handleInfo = new DocumentHandleInfo(task.Handle, fileName, task.CustomData);
                var documentId = identityGenerator.New<DocumentDescriptorId>();

                var createDocument = new InitializeDocumentDescriptor(
                    documentId,
                    blobId,
                    handleInfo,
                    descriptor.Hash,
                    fileName
                );

                _commandBus.Send(createDocument, "import-from-file");

                TaskExecuted(task);
            }
            finally
            {
                TenantContext.Exit();
            }
        }

        private void TaskExecuted(DocumentImportTask task)
        {
            if (task.DeleteAfterImport)
            {
                var fname = task.Uri.LocalPath;
                try
                {
                    File.Delete(fname);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Delete failed: {0}", fname);
                }
            }
        }

        private IIdentityGenerator GetIdentityGeneratorForTenant()
        {
            var tenant = _tenantAccessor.Current;
            if (tenant == NullTenant.Instance)
            {
                return null;
            }

            var container = tenant.Container;
            var generator = container.Resolve<IIdentityGenerator>();
            return generator;
        }

        private IBlobStore GetBlobStoreForTenant()
        {
            var tenant = _tenantAccessor.Current;
            if (tenant == NullTenant.Instance)
            {
                return null;
            }

            var container = tenant.Container;
            var blobStore = container.Resolve<IBlobStore>();
            return blobStore;
        }

        internal DocumentImportTask LoadTask(string pathToFile)
        {
            try
            {
                var asJson = File.ReadAllText(pathToFile)
                    .Replace("%CURRENT_DIR%", Path.GetDirectoryName(pathToFile).Replace("\\", "/"));

                var task = JsonConvert.DeserializeObject<DocumentImportTask>(asJson, PocoSerializationSettings.Default);
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

    public class ImportFileFromFileSystemRunner : IStartable
    {
        private ImportFormatFromFileQueue _job;
        private ManualResetEvent _stop;
        private volatile bool _stopPending;
        public ILogger Logger { get; set; }

        public ImportFileFromFileSystemRunner(ImportFormatFromFileQueue job)
        {
            _job = job;
        }

        public void Start()
        {
            _stop = new ManualResetEvent(false);
            var thread = new Thread(Run);
            thread.Start();
        }

        public void Stop()
        {
            _stopPending = true;
            _stop.WaitOne(TimeSpan.FromSeconds(60));
        }

        private void Run()
        {
            while (!_stopPending)
            {
                Thread.Sleep(60000);
                try
                {
                    _job.PollFileSystem();
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "error polling filesystem");
                }
            }

            _stop.Set();
        }
    }
}

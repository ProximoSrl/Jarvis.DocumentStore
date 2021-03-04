using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Shared.Helpers;
using Jarvis.DocumentStore.Shared.Serialization;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.Commands;
using Jarvis.Framework.Shared.Helpers;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Directory = Jarvis.DocumentStore.Shared.Helpers.DsDirectory;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Core.BackgroundTasks
{
    internal class DocumentImportTask
    {
        private String _fileName;

        /* input */
        public Uri Uri { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public DocumentFormat Format { get; private set; }
        public TenantId Tenant { get; private set; }

        /// <summary>
        /// <para>
        /// If different from null, this contains the name of the 
        /// file that will be loaded in <see cref="IBlobStore"/> if null
        /// file name will be taken from <see cref="DocumentImportTask.Uri"/>.
        /// </para>
        /// <para>
        /// This is useful if you want to avoid name clashing, so you can use guid
        /// for file name to avoid clash on the file system but you want to specify
        /// a real name in this property.
        /// </para>
        /// </summary>
        public String FileName
        {
            get { return _fileName; }
            private set
            {
                _fileName = value;
                if (!String.IsNullOrEmpty(_fileName))
                {
                    _fileName = _fileName.ToSafeFileName('_');
                }
            }
        }

        public DocumentCustomData CustomData { get; private set; }

        public bool DeleteAfterImport { get; private set; }

        /// <summary>
        /// If true we want to import file as reference, we do not want the file
        /// to be stored in <see cref="IBlobStore"/>. If this property is equal to 
        /// true, property <see cref="FileName"/> will be ignored.
        /// </summary>
        public bool ImportAsReference { get; private set; }

        /* working */
        public string PathToTaskFile { get; set; }
        public string Result { get; set; }

        public DateTime FileTimestamp { get; set; }

        internal bool ShouldDeleteFile()
        {
            return DeleteAfterImport && !ImportAsReference;
        }
    }

    public class ImportFailure
    {
        [BsonId]
        public String FileName { get; set; }

        public String Error { get; set; }

        public DateTime Timestamp { get; set; }

        public Int64 ImportFileTimestampTicks { get; set; }
    }

    public class ImportFormatFromFileQueue
    {
        public const string JobExtension = "*.dsimport";
        public ILogger Logger { get; set; }

        private static readonly DocumentFormat OriginalFormat = new DocumentFormat("original");
        private readonly string[] _foldersToWatch;
        private readonly ITenantAccessor _tenantAccessor;
        private readonly ICommandBus _commandBus;
        private readonly ConcurrentDictionary<TenantId, IMongoCollection<ImportFailure>>
            _importFailureCollections = new ConcurrentDictionary<TenantId, IMongoCollection<ImportFailure>>();

        private readonly DocumentStoreConfiguration _configuration;

        internal bool DeleteTaskFileAfterImport { get; set; }

        public ImportFormatFromFileQueue(
            DocumentStoreConfiguration configuration,
            ITenantAccessor tenantAccessor,
            ICommandBus commandBus
        )
        {
            DeleteTaskFileAfterImport = true;
            _configuration = configuration;
            _foldersToWatch = _configuration.FoldersToMonitor;
            _tenantAccessor = tenantAccessor;
            _commandBus = commandBus;
        }

        private Boolean _stopped = false;

        /// <summary>
        /// Stop all filesystem polling
        /// </summary>
        internal void Stop()
        {
            _stopped = true;
        }

        public void PollFileSystem()
        {
            if (_stopped)
            {
                return;
            }

            foreach (var folder in _foldersToWatch)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 4
                };

                var files = Directory.GetFiles(
                    folder,
                    JobExtension,
                    SearchOption.AllDirectories);
                Parallel.ForEach(
                    files,
                    options,
                    file =>
                {
                    if (!_stopped)
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

                            UploadFile(file, task);
                        }
                    }
                });
            }
        }

        internal void UploadFile(String jobFile, DocumentImportTask task)
        {
            String fname = "";
            try
            {
                TenantContext.Enter(task.Tenant);

                if (!task.Uri.IsFile)
                {
                    LogAndThrow("Error importing task file {0}: Uri is not a file: {1}", jobFile, task.Uri);
                }

                fname = task.Uri.LocalPath;

                if (FileHasImportFailureMarker(fname, task.FileTimestamp))
                {
                    return;
                }

                if (!File.Exists(fname))
                {
                    LogAndThrow("Error importing task file {0}: File missing: {1}", jobFile, fname);
                }

                var blobStore = GetBlobStoreForTenant();
                var identityGenerator = GetIdentityGeneratorForTenant();
                if (blobStore == null || identityGenerator == null)
                {
                    Logger.ErrorFormat("Tenant {1} not found or not configured for file: {1}", task.Tenant, fname);
                    return;
                }

                BlobId blobId;
                if (task.ImportAsReference)
                {
                    //this is a different path, we import this as a reference.
                    blobId = blobStore.UploadReference(task.Format, fname);
                }
                else
                {
                    var fileNameWithExtension = !String.IsNullOrEmpty(task.FileName) ?
                        new FileNameWithExtension(task.FileName) :
                        new FileNameWithExtension(fname);

                    //use the real file name from the task not the name of the file
                    using (FileStream fs = File.Open(fname, FileMode.Open, FileAccess.Read))
                    {
                        blobId = blobStore.Upload(task.Format, fileNameWithExtension, fs);
                    }
                }

                //now we need to check, if we have other supported format to load with the 
                //original format.
                object formatPdfValue = null;
                string pdfFileToPreload = null;
                if (task.CustomData?.TryGetValue("format-pdf", out formatPdfValue) == true
                    && formatPdfValue is string)
                {
                    //we have pdf to preload, this  will means that we need to stop office queue
                    //to prevent office queue to generate pdf
                    task.CustomData.Add("disable-queue-office", true);
                    pdfFileToPreload = (String)formatPdfValue;
                }

                if (task.Format == OriginalFormat)
                {
                    var descriptor = blobStore.GetDescriptor(blobId);
                    var fileName = new FileNameWithExtension(task.FileName);
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

                    //if we are creating original format, we should add pdf format if it is
                    //already present in the queue
                    if (!String.IsNullOrEmpty(pdfFileToPreload))
                    {
                        BlobId pdfBlobId = null;
                        if (task.ImportAsReference)
                        {
                            pdfBlobId = blobStore.UploadReference(Client.Model.DocumentFormats.Pdf, pdfFileToPreload);
                        }
                        else
                        {
                            pdfBlobId = blobStore.Upload(Client.Model.DocumentFormats.Pdf, pdfFileToPreload);
                        }

                        //Need to send add format command to actually immediately add pdf format
                        //to this descriptor.
                        var addFormatCommand = new AddFormatToDocumentDescriptor(
                            documentId,
                            Client.Model.DocumentFormats.Pdf,
                            pdfBlobId,
                            PipelineId.Null);
                        _commandBus.Send(addFormatCommand, "import-from-file");
                    }
                }
                else
                {
                    var reader = _tenantAccessor.Current.Container.Resolve<IDocumentWriter>();
                    var handle = reader.FindOneById(task.Handle);
                    var documentId = handle.DocumentDescriptorId;

                    var command = new AddFormatToDocumentDescriptor(
                        documentId,
                        task.Format,
                        blobId,
                        new PipelineId("user-content")
                    );
                    _commandBus.Send(command, "import-from-file");
                }

                TaskExecuted(task);
                DeleteImportFailure(fname);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Job Import Queue - Error importing {0} - {1}", jobFile, ex.Message);
                ImportFailure failure = new ImportFailure()
                {
                    Error = ex.ToString(),
                    FileName = fname,
                    Timestamp = DateTime.Now,
                    ImportFileTimestampTicks = task.FileTimestamp.Ticks,
                };
                MarkImportFailure(failure);
            }
            finally
            {
                TenantContext.Exit();
            }
        }

        private void LogAndThrow(String errorMessage, params Object[] parameters)
        {
            var formattedMessage = String.Format(errorMessage, parameters);
            Logger.Error(formattedMessage);
            throw new ApplicationException(formattedMessage);
        }

        private void MarkImportFailure(ImportFailure failure)
        {
            EnsureFailureConnectionForCurrentTenant();
            _importFailureCollections[_tenantAccessor.Current.Id].Save(failure, failure.FileName);
        }

        private void DeleteImportFailure(String fileName)
        {
            EnsureFailureConnectionForCurrentTenant();
            _importFailureCollections[_tenantAccessor.Current.Id]
                .RemoveById(fileName);
        }

        private Boolean FileHasImportFailureMarker(String fileName, DateTime fileTimestamp)
        {
            EnsureFailureConnectionForCurrentTenant();
            //if files has error 
            return _importFailureCollections[_tenantAccessor.Current.Id]
                .Find(
                    Builders<ImportFailure>.Filter.And(
                        Builders<ImportFailure>.Filter.Eq("_id", fileName),
                        Builders<ImportFailure>.Filter.Eq(i => i.ImportFileTimestampTicks, fileTimestamp.Ticks)
                    ))
                .Project(Builders<ImportFailure>.Projection.Include("_id"))
                .Any();
        }

        private void EnsureFailureConnectionForCurrentTenant()
        {
            if (!_importFailureCollections.ContainsKey(_tenantAccessor.Current.Id))
            {
                var tenantSettings = _configuration.TenantSettings.Single(t => t.TenantId == _tenantAccessor.Current.Id);
                var systemDb = tenantSettings.Get<IMongoDatabase>("system.db");
                _importFailureCollections[_tenantAccessor.Current.Id] =
                    systemDb.GetCollection<ImportFailure>("sys.importFailures");
            }
        }

        private void TaskExecuted(DocumentImportTask task)
        {
            if (task.ShouldDeleteFile())
            {
                var fname = task.Uri.LocalPath;
                try
                {
                    File.Delete(fname);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Delete source failed: {0}", fname);
                }
            }

            if (DeleteTaskFileAfterImport)
            {
                try
                {
                    File.Delete(task.PathToTaskFile);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Delete task failed: {0}", task.PathToTaskFile);
                    throw;
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
                task.FileTimestamp = File.GetLastWriteTimeUtc(pathToFile);
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
        private readonly ImportFormatFromFileQueue _job;
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
            _job.Stop();
            _stopPending = true;
            _stop.WaitOne(TimeSpan.FromSeconds(60));
        }

        private void Run()
        {
            while (!_stopPending)
            {
                //avoiding blocking for 10 seconds during stop.
                for (int i = 0; i < 10; i++)
                {
                    if (_stopPending)
                    {
                        _stop.Set();
                        return;
                    }
                    Thread.Sleep(1000);
                }
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

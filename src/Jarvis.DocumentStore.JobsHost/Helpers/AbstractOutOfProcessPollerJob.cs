using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using System.Timers;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Support;
using Jarvis.DocumentStore.Shared.Jobs;
using Newtonsoft.Json;
using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Text;

namespace Jarvis.DocumentStore.JobsHost.Helpers
{
    public abstract class AbstractOutOfProcessPollerJob : IPollerJob
    {
        public bool IsOutOfProcess
        {
            get { return true; }
        }

        public String QueueName { get; protected set; }

        public String PipelineId { get; protected set; }

        public virtual bool IsActive { get { return true; } }

        public IExtendedLogger Logger { get; set; }

        public JobsHostConfiguration JobsHostConfiguration { get; set; }

        public Boolean Started { get; private set; }

        private readonly String _identity;

        private String _handle;

        readonly JsonSerializerSettings _settings;

        public const Int32 MaxFileNameLength = 220;

        public IClientPasswordSet ClientPasswordSet { get; set; }

        public AbstractOutOfProcessPollerJob()
        {
            _identity = Environment.MachineName + "_" + System.Diagnostics.Process.GetCurrentProcess().Id;
            ClientPasswordSet = NullClientPasswordSet.Instance;
            _settings = new JsonSerializerSettings()
            {
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };
        }

        private List<DsEndpoint> _dsEndpoints;

        protected virtual Int32 ThreadNumber
        {
            get { return 1; }
        }

        public object StringBuider { get; private set; }

        private class DsEndpoint
        {
            public DsEndpoint(string getNextJobUrl, string setJobCompleted, String reQueueJob, Uri baseUrl)
            {
                GetNextJobUrl = getNextJobUrl;
                SetJobCompleted = setJobCompleted;
                ReQueueJob = reQueueJob;
                BaseUrl = baseUrl;
            }

            public String GetNextJobUrl { get; private set; }

            public String SetJobCompleted { get; private set; }

            public String ReQueueJob { get; private set; }

            public Uri BaseUrl { get; set; }
        }

        protected class ProcessResult
        {
            public static readonly ProcessResult Ok;

            static ProcessResult()
            {
                Ok = new ProcessResult(true);
            }

            public static ProcessResult Fail(String errorMessage)
            {
                return new ProcessResult(false)
                {
                    ErrorMessage = errorMessage
                };
            }

            public ProcessResult(Boolean result)
            {
                Result = result;
            }

            public ProcessResult(TimeSpan posticipationTimestamp)
            {
                Result = false;
                Posticipate = true;
                PosticipateExecutionTimestamp = posticipationTimestamp;
            }

            public ProcessResult(TimeSpan posticipationTimestamp, Dictionary<String, String> parametersToModify)
                : this(posticipationTimestamp)
            {
                ParametersToModify = parametersToModify;
            }

            public Boolean Result { get; private set; }

            public Boolean Posticipate { get; private set; }

            /// <summary>
            /// If Result is false, the job could ask to the engine 
            /// to posticipate execution to some time in the future
            /// because probably the queue job could be executed in the
            /// future.
            /// </summary>
            public TimeSpan PosticipateExecutionTimestamp { get; private set; }

            /// <summary>
            /// The job can ask for modification of parameters upon execution.
            /// </summary>
            public Dictionary<String, String> ParametersToModify { get; set; }

            /// <summary>
            /// This property contains the error message if the result is false.
            /// </summary>
            public String ErrorMessage { get; internal set; }
        }

        public void Start(List<String> documentStoreAddressUrls, String handle)
        {
            if (Started) return;
            _handle = handle;
            if (documentStoreAddressUrls.Count == 0) throw new ArgumentException("Component needs at least a document store url", "documentStoreAddressUrls");
            _dsEndpoints = documentStoreAddressUrls
                .Select(addr => new DsEndpoint(
                        addr.TrimEnd('/') + "/queue/getnextjob",
                        addr.TrimEnd('/') + "/queue/setjobcomplete",
                        addr.TrimEnd('/') + "/queue/reQueue",
                        new Uri(addr)))
                .ToList();
            Start(JobsHostConfiguration.QueueJobsPollInterval);
            Started = true;
        }

        public void Stop()
        {
            if (!Started) return;
            Started = false;
            _pollingTimer.Stop();
            _pollingTimer.Dispose();
            _pollingTimer = null;
        }

        private Timer _pollingTimer;

        private void Start(Int32 pollingTimeInMs)
        {
            _pollingTimer = new Timer(pollingTimeInMs);
            _pollingTimer.Elapsed += pollingTimer_Elapsed;
            _pollingTimer.Start();
        }

        private Int32 _numOfPollerTaskActive = 0;

        void pollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _pollingTimer.Stop();

            try
            {
                if (this.ThreadNumber == 1)
                {
                    _numOfPollerTaskActive = 1;
                    ExecuteJobCore();
                }
                else
                {
                    List<Task> taskList = new List<Task>();
                    _numOfPollerTaskActive = ThreadNumber;
                    for (int i = 0; i < ThreadNumber; i++)
                    {
                        var task = Task.Factory.StartNew(ExecuteJobCore);
                        taskList.Add(task);
                    }
                    Task.WaitAll(taskList.ToArray());
                }
            }
            catch (AggregateException aex)
            {
                Logger.ErrorFormat(aex, "Poller error on {0} threads launched {1} exceptions", ThreadNumber, aex.InnerExceptions.Count);
                foreach (var ex in aex.InnerExceptions)
                {
                    Logger.ErrorFormat(ex, "Poller error: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Poller error: {0}", ex.Message);
            }
            finally
            {
                if (Started && _pollingTimer != null) _pollingTimer.Start();
            }
        }

        private void ExecuteJobCore()
        {
            do
            {
                //if half of the task thread finished working, we should end all the pool and restart
                if (ThreadNumber > 2 && _numOfPollerTaskActive < (ThreadNumber / 2))
                {
                    //This can happen because jobs are generated not in block, if we have ex 6 threads
                    //base jobs started 6 tasks, then if in a moment only one task remain only one task remain active
                    //then if the queue manager queue 100 jobs, we have only one thread active. This condition
                    //stops the poll if half of the threads are active, so we need to restart polling with all the tasks.
                    return;
                }
                String workingFolder = null;
                QueuedJobDto nextJob = DsGetNextJob();
                if (nextJob == null)
                {
                    System.Threading.Interlocked.Decrement(ref _numOfPollerTaskActive);
                    return;
                }
                Logger.ThreadProperties["job-id"] = nextJob.Id;
                var baseParameters = ExtractJobParameters(nextJob);
                //remember to enter the right tenant.
                workingFolder = Path.Combine(
                        JobsHostConfiguration.GetWorkingFolder(baseParameters.TenantId, GetType().Name),
                        baseParameters.JobId
                    );
                if (Directory.Exists(workingFolder)) Directory.Delete(workingFolder, true);
                Directory.CreateDirectory(workingFolder);
                try
                {
                    var task = OnPolling(baseParameters, workingFolder);
                    var result = task.Result;
                    if (result.Result)
                    {
                        Logger.DebugFormat("Successfully executed Job: {0}", nextJob.Id);
                    }
                    else
                    {
                        Logger.ErrorFormat("Job {0} completed with errors: {1} with result", nextJob.Id, result.ErrorMessage);
                    }

                    //The execution if failed can be posticipated to future time, probably because the job can retry after a certain
                    //period of time.
                    if (!result.Posticipate)
                    {
                        DsSetJobExecuted(QueueName, nextJob.Id, result.ErrorMessage, result.ParametersToModify);
                    }
                    else
                    {
                        DsReQueueJob(QueueName, nextJob.Id, result.ErrorMessage, result.PosticipateExecutionTimestamp, result.ParametersToModify);
                    }
                }
                catch (AggregateException aex)
                {
                    Logger.ErrorFormat(aex, "Error executing queued job {0} on tenant {1} - {2}",
                        nextJob.Id,
                        nextJob.Parameters[JobKeys.TenantId],
                        aex?.InnerExceptions?[0]?.Message);
                    StringBuilder aggregateMessage = new StringBuilder();
                    aggregateMessage.Append(aex.Message);
                    foreach (var ex in aex.InnerExceptions)
                    {
                        var errorMessage = String.Format("Inner error queued job {0} queue {1}: {2}", nextJob.Id, this.QueueName, ex.Message);
                        Logger.Error(errorMessage, ex);
                        aggregateMessage.Append(errorMessage);
                    }
                    DsSetJobExecuted(QueueName, nextJob.Id, aggregateMessage.ToString(), null);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error executing queued job {0} on tenant {1}", nextJob.Id,
                        nextJob.Parameters[JobKeys.TenantId]);
                    DsSetJobExecuted(QueueName, nextJob.Id, ex.Message, null);
                }
                finally
                {
                    DeleteWorkingFolder(workingFolder);
                    Logger.ThreadProperties["job-id"] = null;
                }
            } while (true); //Exit is in the internal loop
        }

        private void DsSetJobExecuted(string queueName, string jobId, string errorMessage, Dictionary<String, String> parametersToModify)
        {
            string pollerResult;
            using (WebClientEx client = new WebClientEx())
            {
                //TODO: use round robin if a document store is down.
                var firstUrl = _dsEndpoints.First();
                var payload = JsonConvert.SerializeObject(new
                {
                    QueueName = queueName,
                    JobId = jobId,
                    ErrorMessage = errorMessage,
                    ParametersToModify = parametersToModify,
                });
                Logger.DebugFormat("SetJobExecuted url: {0} with payload {1}", firstUrl.SetJobCompleted, payload);
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                pollerResult = client.UploadString(firstUrl.SetJobCompleted, payload);
                Logger.DebugFormat("SetJobExecuted Result: {0}", pollerResult);
            }
        }

        private void DsReQueueJob(string queueName, string jobId, string errorMessage, TimeSpan timeSpan, Dictionary<String, String> parametersToModify)
        {
            string pollerResult;
            using (WebClientEx client = new WebClientEx())
            {
                //TODO: use round robin if a document store is down.
                var firstUrl = _dsEndpoints.First();
                var payload = JsonConvert.SerializeObject(new
                {
                    QueueName = queueName,
                    JobId = jobId,
                    ErrorMessage = errorMessage,
                    ReQueue = true,
                    ReScheduleTimespanInSeconds = (Int32)timeSpan.TotalSeconds,
                    ParametersToModify = parametersToModify
                });
                Logger.DebugFormat("ReQueuedJob url: {0} with payload {1}", firstUrl.SetJobCompleted, payload);
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                pollerResult = client.UploadString(firstUrl.SetJobCompleted, payload);
                Logger.DebugFormat("SetJobExecuted Result: {0}", pollerResult);
            }
        }

        private static PollerJobParameters ExtractJobParameters(QueuedJobDto nextJob)
        {
            PollerJobParameters parameters = new PollerJobParameters();
            parameters.FileExtension = SafeGetParameter(nextJob, JobKeys.FileExtension);
            parameters.FileName = SafeGetParameter(nextJob, JobKeys.FileName);
            parameters.InputDocumentFormat = new DocumentFormat(SafeGetParameter(nextJob, JobKeys.Format));
            parameters.JobId = nextJob.Id;
            parameters.TenantId = SafeGetParameter(nextJob, JobKeys.TenantId);
            parameters.All = nextJob.Parameters;
            return parameters;
        }

        private static string SafeGetParameter(QueuedJobDto nextJob, String parameter)
        {
            if (nextJob.Parameters.ContainsKey(parameter))
                return nextJob.Parameters[parameter];
            return String.Empty;
        }

        private DateTime _lastCommunicationError = DateTime.MinValue;
        private DateTime _lastGoodCommunication = DateTime.Now;

        private QueuedJobDto DsGetNextJob()
        {
            try
            {
                QueuedJobDto nextJob = null;
                string pollerResult;
                using (WebClientEx client = new WebClientEx())
                {
                    //TODO: use round robin if a document store is down.
                    var firstUrl = _dsEndpoints.First();
                    var payload = JsonConvert.SerializeObject(new
                    {
                        QueueName = this.QueueName,
                        Identity = this._identity,
                        Handle = this._handle,
                    });
                    Logger.DebugFormat("Polling url: {0} with payload {1}", firstUrl, payload);
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    pollerResult = client.UploadString(firstUrl.GetNextJobUrl, payload);
                    Logger.DebugFormat("GetNextJobResult: {0}", pollerResult);
                }
                if (!pollerResult.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    nextJob = JsonConvert.DeserializeObject<QueuedJobDto>(pollerResult, _settings);
                }
                _lastGoodCommunication = DateTime.Now;
                return nextJob;
            }
            catch (Exception ex)
            {
                if (DateTime.UtcNow.AddMinutes(-15) > _lastCommunicationError)
                {
                    //new error in 15 minutes, we need to log
                    Logger.ErrorFormat(ex, "Unable to contact Document Store at address: {0}", _dsEndpoints.First());
                    _lastCommunicationError = DateTime.UtcNow;
                }
                else
                {
                    Logger.InfoFormat("Document store cannot be reached, down since {0}", _lastGoodCommunication);
                }
                return null;
            }
        }

        protected async Task<Boolean> AddFormatToDocumentFromFile(
            string tenantId,
            String jobId,
            Client.Model.DocumentFormat format,
            string pathToFile,
            IDictionary<string, object> customData)
        {
            DocumentStoreServiceClient client = GetDocumentStoreClient(tenantId);
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel
            {
                CreatedById = this.PipelineId,
                JobId = jobId,
                QueueName = this.QueueName,
                Format = format,
                PathToFile = pathToFile
            };

            var response = await client.AddFormatToDocument(model, customData).ConfigureAwait(false);
            if (Logger.IsInfoEnabled) Logger.Info($"Job {this.GetType()} Added format {format} to handle with job id {jobId}");

            return response != null;
        }

        protected async Task<Boolean> AddFormatToDocumentFromObject(string tenantId,
              String queueName,
              String jobId,
              Client.Model.DocumentFormat format,
              Object obj,
              String originalFileName,
              IDictionary<string, object> customData)
        {
            DocumentStoreServiceClient client = GetDocumentStoreClient(tenantId);
            AddFormatFromObjectToDocumentModel model = new AddFormatFromObjectToDocumentModel
            {
                CreatedById = this.PipelineId,
                JobId = jobId,
                QueueName = queueName,
                Format = format,
                FileName = originalFileName,
                StringContent = JsonConvert.SerializeObject(obj),
            };
            var response = await client.AddFormatToDocument(model, customData).ConfigureAwait(false);
            return response != null;
        }

        protected async Task<Boolean> AddAttachmentToHandle(
            string tenantId,
            String jobId,
            string pathToFile,
            String source,
            String relativePath,
            IDictionary<string, object> customData)
        {
            DocumentStoreServiceClient client = GetDocumentStoreClient(tenantId);

            var response = await client.UploadAttachmentAsync(pathToFile, this.QueueName, jobId, source, relativePath, customData).ConfigureAwait(false);
            return response != null;
        }

        protected String SanitizeFileNameForLength(String fileName)
        {
            if (fileName.Length > MaxFileNameLength)
            {
                //we need to clip length of the file.
                var oriFileName = fileName;
                var fileExtension = Path.GetExtension(fileName);
                fileName = fileName.Substring(0, MaxFileNameLength - fileExtension.Length) + fileExtension;
                Logger.InfoFormat("Original Filename {0} longer than 260 chars, it is truncated to {1}", oriFileName, fileName);
            }
            return fileName;
        }

        protected async Task<String> DownloadBlob(
            String tenantId,
            String jobId,
            String originalFileName,
            String workingFolder)
        {
            String fileName = Path.Combine(workingFolder, originalFileName);
            //TooLongNames should be truncated, tika, or other libraries are not able to access
            //too long file names. Max file name is 260, but some libraries or task can append some more char
            //ex tika.html
            fileName = SanitizeFileNameForLength(fileName);
            DocumentStoreServiceClient client = GetDocumentStoreClient(tenantId);
            var reader = client.OpenBlobIdForRead(this.QueueName, jobId);
            using (var downloaded = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var stream = await reader.OpenStream().ConfigureAwait(false);
                stream.CopyTo(downloaded);
            }
            Logger.DebugFormat("Downloaded blob for job {0} for tenant {1} in local file {2}", jobId, tenantId, fileName);
            return fileName;
        }

        protected async Task<Stream> GetBlobFormatReader(
            String tenantId,
            String jobId)
        {
            DocumentStoreServiceClient client = GetDocumentStoreClient(tenantId);
            var reader = client.OpenBlobIdForRead(this.QueueName, jobId);
            return await reader.OpenStream();
        }

        protected String[] GetFormats(String tenantId, String jobId)
        {
            using (WebClientEx client = new WebClientEx())
            {
                //TODO: use round robin if a document store is down.
                var url = GetBlobUriForJobFormats(tenantId, jobId);
                var result = client.DownloadString(url);
                var formats = JsonConvert.DeserializeObject<String[]>(result);
                return formats;
            }
        }

        protected DocumentStoreServiceClient GetDocumentStoreClient(string tenantId)
        {
            return new DocumentStoreServiceClient(_dsEndpoints.First().BaseUrl, tenantId);
        }

        protected String GetBlobUriForJobBlob(String tenantId, String jobId)
        {
            return String.Format("{0}/{1}/documents/jobs/blob/{2}/{3}",
                _dsEndpoints.First().BaseUrl,
                tenantId,
                QueueName,
                jobId);
        }

        protected String GetBlobUriForJobFormats(String tenantId, String jobId)
        {
            return String.Format("{0}/{1}/documents/jobs/formats/{2}/{3}",
                _dsEndpoints.First().BaseUrl,
                tenantId,
                QueueName,
                jobId);
        }

        private void DeleteWorkingFolder(String workingFolder)
        {
            if (!String.IsNullOrEmpty(workingFolder))
            {
                try
                {
                    if (Directory.Exists(workingFolder))
                        Directory.Delete(workingFolder, true);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error deleting {0}", workingFolder);
                }
            }
        }

        protected abstract Task<ProcessResult> OnPolling(
            PollerJobParameters parameters,
            String workingFolder);
    }
}

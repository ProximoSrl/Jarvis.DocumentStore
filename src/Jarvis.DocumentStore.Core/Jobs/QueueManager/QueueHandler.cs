using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.Helpers;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{

    /// <summary>
    /// This is a simple handler manager for a single queue, this component 
    /// is used to manage the queue for a single job. This class has a reference
    /// on the collection used to manage jobs.
    /// </summary>
    public class QueueHandler
    {
        private readonly IMongoCollection<QueuedJob> _collection;
        private readonly QueueInfo _info;
        private readonly BsonDocument _statsAggregationQuery;

        private readonly MetricHeartBeatHealthCheck _healthCheck;

        public ILogger Logger { get; set; }
        public String Name { get; private set; }

        public QueueHandler(QueueInfo info, IMongoDatabase database)
        {
            _collection = database.GetCollection<QueuedJob>("queue." + info.Name);
            _collection.Indexes.CreateOne(
                Builders<QueuedJob>.IndexKeys.Ascending(x => x.Status).Ascending(x => x.StreamId).Ascending(x => x.SchedulingTimestamp),
                new CreateIndexOptions() { Name = "ForGetNextJobQuery" });

            _collection.Indexes.CreateOne(
               Builders<QueuedJob>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.BlobId),
                new CreateIndexOptions() { Name = "TenantAndBlob", Unique = false });

            _collection.Indexes.CreateOne(
             Builders<QueuedJob>.IndexKeys.Ascending(x => x.Handle).Ascending(x => x.Status),
              new CreateIndexOptions() { Name = "HandleAndStatus", Unique = false });

            _info = info;
            Name = info.Name;

            _statsAggregationQuery = BsonDocument.Parse(@" 
       { 
          _id : '$Status',
          c : {$sum:1}
       }");
            //timeout of polling time is the maximum timeout allowed before a job is considered to be locked
            //but give 10 seconds to each job to start
            var millisecondTimeout = info.JobLockTimeout * 60 * 1000;
            _healthCheck = MetricHeartBeatHealthCheck.Create(
                "Job queue " + info.Name,
                millisecondTimeout,
                TimeSpan.FromMilliseconds(millisecondTimeout - 10000));

            Logger = NullLogger.Instance;
        }

        /// <summary>
        /// Handle a <see cref="StreamReadModel" /> and generates job for the queue
        /// if needed.
        /// </summary>
        /// <param name="streamElement"></param>
        /// <param name="tenantId"></param>
        /// <param name="forceReSchedule"></param>
        public QueuedJobId Handle(
            StreamReadModel streamElement,
            TenantId tenantId,
            Boolean forceReSchedule = false)
        {
            if (_info.ShouldCreateJob(streamElement))
            {
                if (!forceReSchedule)
                {
                    //look for already existing job with the same blobid, there is no need to re-queue again
                    //because if a job with the same blobid was already fired for this queue there is no need
                    //to re-issue
                    var existing = _collection.Find(
                        Builders<QueuedJob>.Filter.And(
                            Builders<QueuedJob>.Filter.Eq(j => j.BlobId, streamElement.FormatInfo.BlobId),
                            Builders<QueuedJob>.Filter.Eq(j => j.TenantId, tenantId)
                        )
                    ).Count() > 0;
                    if (existing)
                    {
                        return null;
                    }
                }
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info($"Queue {_info.Name} CREATE JOB to process {streamElement.Describe()}");
                }

                QueuedJob job = new QueuedJob();
                job.Id = new QueuedJobId(Guid.NewGuid().ToString());
                job.SchedulingTimestamp = DateTime.Now;
                job.StreamId = streamElement.Id;
                job.TenantId = tenantId;
                job.DocumentDescriptorId = streamElement.DocumentDescriptorId;
                job.BlobId = streamElement.FormatInfo.BlobId;
                job.Handle = new DocumentHandle(streamElement.Handle);
                job.Parameters = new Dictionary<string, string>();
                job.Parameters.Add(JobKeys.FileExtension, streamElement.Filename.Extension);
                job.Parameters.Add(JobKeys.Format, streamElement.FormatInfo.DocumentFormat);
                job.Parameters.Add(JobKeys.FileName, streamElement.Filename);
                job.Parameters.Add(JobKeys.TenantId, tenantId);
                job.Parameters.Add(JobKeys.MimeType, MimeTypes.GetMimeType(streamElement.Filename));
                job.Parameters.Add(JobKeys.PipelineId, streamElement.FormatInfo?.PipelineId?.ToString());
                if (forceReSchedule)
                {
                    job.Parameters.Add(JobKeys.Force, "true");
                }
                job.HandleCustomData = streamElement.DocumentCustomData;
                if (_info.Parameters != null)
                {
                    foreach (var parameter in _info.Parameters)
                    {
                        job.Parameters.Add(parameter.Key, parameter.Value);
                    }
                }

                _collection.InsertOne(job);

                return job.Id;
            }
            else
            {
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug($"Queue {_info.Name} do not need to process {streamElement.Describe()}");
                }
            }
            return null;
        }

        /// <summary>
        /// This function add a job from external code. Usually jobs are automatically
        /// created after an handle is loaded in document store or after a new 
        /// format is present, but some queue, as PdfComposer, creates job only from
        /// manual input.
        /// </summary>
        /// <param name="job"></param>
        public Boolean QueueJob(QueuedJob job)
        {
            _collection.InsertOne(job);
            return true;
        }

        public QueuedJob GetNextJob(
            String identity,
            String handle,
            TenantId tenantId,
            Dictionary<String, Object> parameterOrCustomDataFilter)
        {
            if (_healthCheck != null)
            {
                _healthCheck.Pulse();
            }

            var query = Builders<QueuedJob>.Filter.And(
                Builders<QueuedJob>.Filter.Or(
                    Builders<QueuedJob>.Filter.Eq(j => j.Status, QueuedJobExecutionStatus.Idle),
                    Builders<QueuedJob>.Filter.Eq(j => j.Status, QueuedJobExecutionStatus.ReQueued)
                ),
                Builders<QueuedJob>.Filter.Lte(j => j.SchedulingTimestamp, DateTime.Now)
            );
            if (tenantId?.IsValid() == true)
            {
                query = Builders<QueuedJob>.Filter.And(query, Builders<QueuedJob>.Filter.Eq(j => j.TenantId, tenantId));
            }
            if (parameterOrCustomDataFilter?.Count > 0)
            {
                foreach (var filter in parameterOrCustomDataFilter)
                {
                    query = Builders<QueuedJob>.Filter.And(query,
                        Builders<QueuedJob>.Filter.Or(
                            Builders<QueuedJob>.Filter.Eq("HandleCustomData." + filter.Key, filter.Value),
                            Builders<QueuedJob>.Filter.Eq("Parameters." + filter.Key, filter.Value)));
                }
            }

            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<QueuedJob>();
            var renderedFilter = query.Render(documentSerializer, BsonSerializer.SerializerRegistry);
            var json = renderedFilter.ToJson();

            var result = _collection.FindOneAndUpdate(
                query,
                 //SortBy = SortBy<QueuedJob>.Ascending(j => j.SchedulingTimestamp),
                 Builders<QueuedJob>.Update
                    .Set(j => j.Status, QueuedJobExecutionStatus.Executing)
                    .Set(j => j.ExecutionStartTime, null)
                    .Set(j => j.ExecutingIdentity, identity)
                    .Set(j => j.ExecutingHandle, handle)
                    .Set(j => j.ExecutionError, null),
                 new FindOneAndUpdateOptions<QueuedJob, QueuedJob>()
                 {
                     Sort = Builders<QueuedJob>.Sort
                        .Ascending(j => j.Status) //idle is 0, they will get executed with priority.
                        .Ascending(j => j.SchedulingTimestamp),
                     ReturnDocument = ReturnDocument.After
                 });
            return result;
        }

        public Boolean SetJobExecuted(
            String jobId,
            String errorMessage,
            Dictionary<String, String> parametersToModify)
        {
            var job = _collection.FindOneById(BsonValue.Create(jobId));
            if (job == null)
            {
                Logger.Error($"Request SetJobExecuted for unexisting job id {jobId} for queue {_info.Name}");
                return false;
            }
            SetJobExecutionStatus(job, errorMessage);
            if (parametersToModify != null)
            {
                foreach (var parameter in parametersToModify)
                {
                    job.Parameters[parameter.Key] = parameter.Value;
                }
            }

            job.ExecutionEndTime = DateTime.Now;

            _collection.ReplaceOne(
                 Builders<QueuedJob>.Filter.Eq("_id", job.Id),
                 job,
                 new UpdateOptions { IsUpsert = true });
            return true;
        }

        internal bool ReQueueJob(string jobId, string errorMessage, TimeSpan timeSpan, Dictionary<String, String> parametersToModify)
        {
            var job = _collection.FindOneById(BsonValue.Create(jobId));
            if (job == null)
            {
                Logger.Error($"Request ReQueueJob for unexisting job id {jobId} for queue {_info.Name}");
                return false;
            }
            SetJobExecutionStatus(job, errorMessage);
            job.Status = QueuedJobExecutionStatus.ReQueued;
            job.SchedulingTimestamp = DateTime.Now.Add(timeSpan);
            if (parametersToModify != null)
            {
                foreach (var parameter in parametersToModify)
                {
                    job.Parameters[parameter.Key] = parameter.Value;
                }
            }

            _collection.ReplaceOne(
                 Builders<QueuedJob>.Filter.Eq("_id", job.Id),
                 job);
            return true;
        }

        /// <summary>
        /// Return the list of all jobs that are blocked, a job is blocked if it is in
        /// Executing state more than a certain amount of time.
        /// </summary>
        /// <returns>List of jobs that are blocked.</returns>
        internal List<QueuedJob> GetBlockedJobs()
        {
            var limit = DateTime.Now.AddMinutes(-_info.JobLockTimeout);
            var existing = _collection.Find(
                Builders<QueuedJob>.Filter.And(
                     Builders<QueuedJob>.Filter.Eq(j => j.Status, QueuedJobExecutionStatus.Executing),
                     Builders<QueuedJob>.Filter.Lt(j => j.ExecutionStartTime, limit)
                )).ToList();

            return existing;
        }

        /// <summary>
        /// when a document descriptor is deleted there is no need to continue
        /// scheduling jobs, so all jobs should be deleted.
        /// </summary>
        /// <param name="documentDescriptorId"></param>
        internal void DeletedJobForDescriptor(DocumentDescriptorId documentDescriptorId)
        {
            _collection.DeleteMany(
                Builders<QueuedJob>.Filter.Eq(j => j.DocumentDescriptorId, documentDescriptorId));
        }

        internal IEnumerable<QueueStatInfo> GetStatus()
        {
            return _collection.Aggregate()
                .Group(_statsAggregationQuery)
                .ToList()
                .Select(x => new QueueStatInfo(
                    (QueuedJobExecutionStatus)Convert.ToInt32(x["_id"]),
                     Convert.ToInt32(x["c"]))
            ).OrderBy(x => x.Status);
        }

        internal QueuedJob GetJob(string jobId)
        {
            return _collection.FindOneById(jobId);
        }

        internal Boolean ReScheduleFailed()
        {
            var result = _collection.UpdateMany(
                 Builders<QueuedJob>.Filter.Eq(j => j.Status, QueuedJobExecutionStatus.Failed),
                 Builders<QueuedJob>.Update
                    .Set(j => j.Status, QueuedJobExecutionStatus.ReQueued)
                    .Set(j => j.SchedulingTimestamp, DateTime.Now));
            return result.MatchedCount > 0;
        }

        internal Boolean HasPendingJob(TenantId tenantId, DocumentHandle handle)
        {
            return _collection.Find(
                Builders<QueuedJob>.Filter.And(
                    Builders<QueuedJob>.Filter.Eq(j => j.Handle, handle),
                    Builders<QueuedJob>.Filter.Eq(j => j.TenantId, tenantId),
                    Builders<QueuedJob>.Filter.Ne(j => j.Status, QueuedJobExecutionStatus.Succeeded),
                    Builders<QueuedJob>.Filter.Ne(j => j.Status, QueuedJobExecutionStatus.Failed)
                ))
                .Limit(1)
                .Any();
        }

        /// <summary>
        /// Set job execution status.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="errorMessage">if this parameter is not empty or null the job result 
        /// will be set to failed.</param>
        private void SetJobExecutionStatus(QueuedJob job, String errorMessage)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                job.ErrorCount++;
                job.ExecutionError = errorMessage;
                if (job.ErrorCount >= _info.MaxNumberOfFailure)
                {
                    job.Status = QueuedJobExecutionStatus.Failed;
                }
                else
                {
                    job.Status = QueuedJobExecutionStatus.ReQueued;
                }
                job.SchedulingTimestamp = DateTime.Now; //this will move failing job to the end of the queue.
            }
            else
            {
                job.Status = QueuedJobExecutionStatus.Succeeded;
            }
        }

        /// <summary>
        /// Return all jobs that are present in the system for a specific handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public QueuedJob[] GetJobsForHandle(TenantId tenantId, DocumentHandle handle)
        {
            return _collection.Find(
                Builders<QueuedJob>.Filter.And(
                    Builders<QueuedJob>.Filter.Eq(j => j.Handle, handle),
                    Builders<QueuedJob>.Filter.Eq(j => j.TenantId, tenantId)
                ))
                .ToEnumerable()
                .ToArray();
        }
    }

    public sealed class QueueStatInfo
    {
        public QueuedJobExecutionStatus Status { get; private set; }

        public long Count { get; private set; }

        public QueueStatInfo(QueuedJobExecutionStatus status, long count)
        {
            Status = status;
            Count = count;
        }
    }
}

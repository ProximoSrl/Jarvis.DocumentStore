using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Driver.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.Framework.Shared.Helpers;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{

    /// <summary>
    /// This is a simple handler for queue that creates jobs in queues based on settings.
    /// </summary>
    public class QueueHandler
    {
        readonly IMongoCollection<QueuedJob> _collection;
        readonly QueueInfo _info;
        readonly BsonDocument _statsAggregationQuery;

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
        public void Handle(
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
                        Builders< QueuedJob>.Filter.And(
                            Builders< QueuedJob>.Filter.Eq(j => j.BlobId, streamElement.FormatInfo.BlobId),
                            Builders<QueuedJob>.Filter.Eq(j => j.TenantId, tenantId)
                        )
                    ).Count() > 0;
                    if (existing) return;
                }
                if (Logger.IsDebugEnabled) Logger.DebugFormat("Create queue for readmodel stream id {0} and queue {1}", streamElement.Id, _info.Name);
                QueuedJob job = new QueuedJob();
                var id = new QueuedJobId(Guid.NewGuid().ToString());
                job.Id = id;
                job.SchedulingTimestamp = DateTime.Now;
                job.StreamId = streamElement.Id;
                job.TenantId = tenantId;
                job.DocumentDescriptorId = streamElement.DocumentDescriptorId;
                job.BlobId = streamElement.FormatInfo.BlobId;
                job.Handle = new DocumentHandle( streamElement.Handle);
                job.Parameters = new Dictionary<string, string>();
                job.Parameters.Add(JobKeys.FileExtension, streamElement.Filename.Extension);
                job.Parameters.Add(JobKeys.Format, streamElement.FormatInfo.DocumentFormat);
                job.Parameters.Add(JobKeys.FileName, streamElement.Filename);
                job.Parameters.Add(JobKeys.TenantId, tenantId);
                job.Parameters.Add(JobKeys.MimeType, MimeTypes.GetMimeType(streamElement.Filename));
                job.HandleCustomData = streamElement.DocumentCustomData;
                if (_info.Parameters != null) 
                {
                    foreach (var parameter in _info.Parameters)
                    {
                        job.Parameters.Add(parameter.Key, parameter.Value);
                    }
                }

                _collection.InsertOne(job);
            }
        }

        public QueuedJob GetNextJob(String identity, String handle, TenantId tenantId, Dictionary<String, Object> customData)
        {
            if (_healthCheck != null) _healthCheck.Pulse();
            var query = Builders<QueuedJob>.Filter.Or(
                    Builders< QueuedJob>.Filter.Eq(j => j.Status, QueuedJobExecutionStatus.Idle),
                    Builders<QueuedJob>.Filter.Eq(j => j.Status, QueuedJobExecutionStatus.ReQueued)
                );
            if (tenantId != null && tenantId.IsValid()) 
            {
                query = Builders<QueuedJob>.Filter.And(query, Builders<QueuedJob>.Filter.Eq(j => j.TenantId, tenantId));
            }
            if (customData != null && customData.Count > 0) 
            {
                foreach (var filter in customData)
                {
                    query = Builders<QueuedJob>.Filter.And(query, Builders<QueuedJob>.Filter.Eq("HandleCustomData." + filter.Key, BsonValue.Create( filter.Value)));
                }
            }
            var result = _collection.FindOneAndUpdate(
                query,
                 //SortBy = SortBy<QueuedJob>.Ascending(j => j.SchedulingTimestamp),
                 Builders<QueuedJob>.Update
                    .Set(j => j.Status, QueuedJobExecutionStatus.Executing)
                    .Set(j => j.ExecutionStartTime, DateTime.Now)
                    .Set(j => j.ExecutingIdentity, identity)
                    .Set(j => j.ExecutingHandle, handle)
                    .Set(j => j.ExecutionError, null),
                 new FindOneAndUpdateOptions<QueuedJob, QueuedJob>() {
                     Sort = Builders<QueuedJob>.Sort.Ascending(j => j.SchedulingTimestamp),
                     ReturnDocument = ReturnDocument.After
                 });
            if (result != null)
            {
                return result;
            }
            return null;
            throw new ApplicationException("Error in Finding next job.");
        }

       
        public Boolean SetJobExecuted(String jobId, String errorMessage) 
        {
            var job = _collection.FindOneById(BsonValue.Create(jobId));
            if (job == null) 
            {
                Logger.ErrorFormat("Request SetJobExecuted for unexisting job id {0}", jobId);
                return false;
            }
            if (!String.IsNullOrEmpty(errorMessage)) 
            {
                job.ErrorCount += 1;
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
            job.ExecutionEndTime = DateTime.Now;

            _collection.ReplaceOne(
                 Builders<QueuedJob>.Filter.Eq("_id", job.Id),
                 job,
                 new UpdateOptions { IsUpsert = true });
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
                Builders< QueuedJob>.Filter.And(
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

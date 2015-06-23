using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
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

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{

    /// <summary>
    /// This is a simple handler for queue that creates jobs in queues based on settings.
    /// </summary>
    public class QueueHandler
    {
        readonly MongoCollection<QueuedJob> _collection;
        readonly QueueInfo _info;
        readonly BsonDocument _statsAggregationQuery;
        private readonly AggregateArgs _aggregation;

        public ILogger Logger { get; set; }
        public String Name { get; private set; }

        public QueueHandler(QueueInfo info, MongoDatabase database)
        {
            _collection = database.GetCollection<QueuedJob>("queue." + info.Name);
            _collection.CreateIndex(
                IndexKeys<QueuedJob>.Ascending(x => x.Status, x => x.StreamId, x => x.SchedulingTimestamp),
                IndexOptions.SetName("ForGetNextJobQuery"));
            //This index was unique to avoid same job to be executed for
            //same blob and tenant, but when we introduced the ability to
            //re-schedule a job, this constraint was removed.
            if (_collection.IndexExistsByName("UniqueTenantAndBlob"))
            {
                _collection.DropIndexByName("UniqueTenantAndBlob");
            }

            _collection.CreateIndex(
               IndexKeys<QueuedJob>.Ascending(x => x.TenantId, x => x.BlobId),
               IndexOptions.SetName("TenantAndBlob").SetUnique(false));
            _info = info;
            Name = info.Name;

            _statsAggregationQuery = BsonDocument.Parse(@"
{   $group : 
       { 
          _id : '$Status',
          c : {$sum:1}
       }
}");

            _aggregation = new AggregateArgs()
            {
                Pipeline = new[] { _statsAggregationQuery }
            };
            Logger = NullLogger.Instance;
        }

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
                        Query.And(
                            Query<QueuedJob>.EQ(j => j.BlobId, streamElement.FormatInfo.BlobId),
                            Query<QueuedJob>.EQ(j => j.TenantId, tenantId)
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
                job.DocumentId = streamElement.DocumentId;
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

                _collection.Save(job);
            }
        }

        public QueuedJob GetNextJob(String identity, String handle, TenantId tenantId, Dictionary<String, Object> customData)
        {
            IMongoQuery query = Query.Or(
                    Query<QueuedJob>.EQ(j => j.Status, QueuedJobExecutionStatus.Idle),
                    Query<QueuedJob>.EQ(j => j.Status, QueuedJobExecutionStatus.ReQueued)
                );
            if (tenantId != null && tenantId.IsValid()) 
            {
                query = Query.And(query, Query<QueuedJob>.EQ(j => j.TenantId, tenantId));
            }
            if (customData != null && customData.Count > 0) 
            {
                foreach (var filter in customData)
                {
                    query = Query.And(query, Query.EQ("HandleCustomData." + filter.Key, BsonValue.Create( filter.Value)));
                }
            }
            var result = _collection.FindAndModify(new FindAndModifyArgs()
            {
                Query = query,
                SortBy = SortBy<QueuedJob>.Ascending(j => j.SchedulingTimestamp),
                Update = Update<QueuedJob>
                    .Set(j => j.Status, QueuedJobExecutionStatus.Executing)
                    .Set(j => j.ExecutionStartTime, DateTime.Now)
                    .Set(j => j.ExecutingIdentity, identity)
                    .Set(j => j.ExecutingHandle, handle)
                    .Set(j => j.ExecutionError, null)
            });
            if (result.Ok)
            {
                if (result.Response["value"] is BsonNull) return null;
                return _collection.FindOneById(BsonValue.Create(result.Response["value"]["_id"]));
            }
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
            _collection.Save(job);
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
                Query.And(
                    Query<QueuedJob>.EQ(j => j.Status, QueuedJobExecutionStatus.Executing),
                    Query<QueuedJob>.LT(j => j.ExecutionStartTime, limit)
                )).ToList();

            return existing;
        }

        internal IEnumerable<QueueStatInfo> GetStatus()
        {
            return _collection.Aggregate(_aggregation)
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
            var result = _collection.Update(
                 Query<QueuedJob>.EQ(j => j.Status, QueuedJobExecutionStatus.Failed),
                 Update<QueuedJob>
                    .Set(j => j.Status, QueuedJobExecutionStatus.ReQueued)
                    .Set(j => j.SchedulingTimestamp, DateTime.Now),
                 UpdateFlags.Multi);
            return result.Ok;
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

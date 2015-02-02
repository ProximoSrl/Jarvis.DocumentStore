using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Jobs;
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

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{

    public class QueueInfo 
    {
        /// <summary>
        /// name of the queue
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// It is a .NET regex
        /// If different from null it contains a filter that permits to queue jobs
        /// only if the <see cref="StreamReadModel" /> is generated from a specific
        /// pipeline.
        /// Remember that negative match can be expressed by this one
        /// ^(?!office$|tika$).* http://stackoverflow.com/questions/6830796/regex-to-match-anything-but-two-words
        /// </summary>
        public String Pipeline { get; private set; }

        /// <summary>
        /// It is a pipe separated list of desired extension.
        /// </summary>
        public String Extension { get; private set; }

        private String[] _splittedExtension;

        public Dictionary<String, String> Parameters { get; set; }

        public int MaxNumberOfFailure { get; set; }

        public QueueInfo(
                String name,
                String pipeline,
                String extension
            )
        {
            Name = name;
            Pipeline = pipeline;
            Extension = extension;
            if (!String.IsNullOrEmpty(Extension))
                _splittedExtension = Extension.Split('|');
            else
                _splittedExtension = new string[] { };
            MaxNumberOfFailure = 5;
        }

        internal bool ShouldCreateJob(StreamReadModel streamElement)
        {
            if (_splittedExtension.Length > 0 && !_splittedExtension.Contains(streamElement.Filename.Extension)) 
                return false;

            if (!String.IsNullOrEmpty(Pipeline) &&
                !Regex.IsMatch(streamElement.FormatInfo.PipelineId, Pipeline))
                return false;

            return true;
        }


    }

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
                IndexKeys<QueuedJob>.Ascending(x => x.Status, x => x.StreamId));
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

        public void Handle(StreamReadModel streamElement, TenantId tenantId)
        {
            if (_info.ShouldCreateJob(streamElement)) 
            {
                var id = streamElement.FormatInfo.BlobId + "_" + tenantId;
                //look for already existing job with the same blobid, there is no need to re-queue again
                //because if a job with the same blobid was already fired for this queue there is no need
                //to re-issue
                var existing = _collection.Find(Query<QueuedJob>.EQ(j => j.Id, id)).Count() > 0;
                if (existing) return;

                QueuedJob job = new QueuedJob();
                job.Id = id;
                job.CreationTimestamp = DateTime.Now;
                job.StreamId = streamElement.Id;
                job.TenantId = tenantId;
                job.Parameters = new Dictionary<string, string>();
                job.Parameters.Add(JobKeys.FileExtension, streamElement.Filename.Extension);
                job.Parameters.Add(JobKeys.DocumentId, streamElement.DocumentId);
                job.Parameters.Add(JobKeys.Format, streamElement.FormatInfo.DocumentFormat);
                job.Parameters.Add(JobKeys.BlobId, streamElement.FormatInfo.BlobId);
                job.Parameters.Add(JobKeys.FileName, streamElement.Filename);
                job.Parameters.Add(JobKeys.TenantId, tenantId);
                job.HandleCustomData = streamElement.HandleCustomData;
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

        public QueuedJob GetNextJob(String identity, String handle)
        {
            var result = _collection.FindAndModify(new FindAndModifyArgs()
            {
                Query =  Query.Or(
                    Query<QueuedJob>.EQ(j => j.Status, QueuedJobExecutionStatus.Idle),
                    Query<QueuedJob>.EQ(j => j.Status, QueuedJobExecutionStatus.ReQueued)
                ),
                SortBy = SortBy<QueuedJob>.Ascending(j => j.StreamId),
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
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns>List of jobs that are blocked.</returns>
        internal List<QueuedJob> GetBlockedJobs(int timeoutInMilliseconds)
        {
            var limit = DateTime.Now.AddMilliseconds(-timeoutInMilliseconds);
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
                    (QueuedJobExecutionStatus)x["_id"].AsInt32,
                     x["c"].AsInt32)
            ).OrderBy(x => x.Status);
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

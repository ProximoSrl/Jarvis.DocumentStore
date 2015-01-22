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
        MongoCollection<QueuedJob> _collection;
        QueueInfo _info;

        public String Name { get; private set; }

        public QueueHandler(QueueInfo info, MongoDatabase database)
        {
            _collection = database.GetCollection<QueuedJob>("queue." + info.Name);
            _collection.CreateIndex(
                IndexKeys<QueuedJob>.Ascending(x => x.Finished, x => x.Executing, x => x.StreamId));
            _info = info;
            Name = info.Name;
        }

        public void Handle(StreamReadModel streamElement, TenantId tenantId)
        {
            if (_info.ShouldCreateJob(streamElement)) 
            {
                QueuedJob job = new QueuedJob();
                job.Id = streamElement.Id + "_" + tenantId;
                job.CreationTimestamp = DateTime.Now;
                job.StreamId = streamElement.Id;
                job.TenantId = tenantId;
                job.Parameters = new Dictionary<string, string>();
                if (streamElement.FormatInfo != null && streamElement.FormatInfo.BlobId != null)
                    job.Parameters.Add(JobKeys.BlobId, streamElement.FormatInfo.BlobId);
                job.Parameters.Add(JobKeys.DocumentId, streamElement.DocumentId);

                _collection.Save(job);
            }
        }

        public QueuedJob GetNextJob()
        {
            var result = _collection.FindAndModify(new FindAndModifyArgs()
            {
                Query =  Query.And(
                    Query<QueuedJob>.NE(j => j.Finished, true),
                    Query<QueuedJob>.NE(j => j.Executing, true)
                ),
                SortBy = SortBy<QueuedJob>.Ascending(j => j.StreamId),
                Update = Update<QueuedJob>
                    .Set(j => j.Executing, true)
                    .Set(j => j.ExecutionStartTime, DateTime.Now)
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
            var result = _collection.FindAndModify(new FindAndModifyArgs()
            {
                Query = Query.And(
                    Query<QueuedJob>.EQ(j => j.Id, jobId)
                ),
                SortBy = SortBy<QueuedJob>.Ascending(j => j.Id),
                Update = Update<QueuedJob>
                    .Set(j => j.Executing, false)
                    .Set(j => j.ExecutionEndTime, DateTime.Now)
                    .Set(j => j.ExecutionError, errorMessage)
                    .Set(j => j.Finished, true)
            });
            return result.Ok && !(result.Response["value"] is BsonNull);
        }
    }
}

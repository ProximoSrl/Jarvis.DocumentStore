using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.ReadModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public interface IQueueHandler 
    {
        void Handle(StreamReadModel streamElement, TenantId tenantId);
    }

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

    public class QueuedJob 
    {
        public String Id { get; set; }

        public Int64 StreamId { get; set; }

        public TenantId TenantId { get; set; }

        public DateTime CreationDate { get; set; }
    }

    /// <summary>
    /// This is a simple handler for queue.
    /// </summary>
    public class QueueHandler : IQueueHandler
    {
        MongoCollection<QueuedJob> _collection;
        QueueInfo _info;

        public QueueHandler(QueueInfo info, MongoDatabase database)
        {
            _collection = database.GetCollection<QueuedJob>("queue." + info.Name);
            _info = info;
        }

        public void Handle(StreamReadModel streamElement, TenantId tenantId)
        {
            if (_info.ShouldCreateJob(streamElement)) 
            {
                QueuedJob job = new QueuedJob();
                job.Id = streamElement.Id + "_" + tenantId;
                job.CreationDate = DateTime.Now;
                job.StreamId = streamElement.Id;
                job.TenantId = tenantId;
                _collection.Save(job);
            }
        }
    }
}

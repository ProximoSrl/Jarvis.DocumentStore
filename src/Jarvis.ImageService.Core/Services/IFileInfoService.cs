using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using MongoDB.Driver;

namespace Jarvis.ImageService.Core.Services
{
    public interface IFileInfoService
    {
        FileInfo GetById(string id);
        void Save(FileInfo fileInfo);
        void Create(string id, string filename);
    }

    public class MongoDbFileInfoService : IFileInfoService
    {
        readonly IPipelineScheduler _scheduler;
        readonly MongoCollection<FileInfo> _collection;

        public MongoDbFileInfoService(MongoDatabase db, IPipelineScheduler scheduler)
        {
            _scheduler = scheduler;
            _collection = db.GetCollection<FileInfo>("fileinfo");
        }

        public FileInfo GetById(string id)
        {
            return _collection.FindOneById(id.ToLowerInvariant());
        }

        public void Save(FileInfo fileInfo)
        {
            _collection.Save(fileInfo);
        }

        public void Create(string id, string filename)
        {
            var fi = new FileInfo(id, filename);
            Save(fi);
            _scheduler.QueueThumbnail(id);
        }
    }
}

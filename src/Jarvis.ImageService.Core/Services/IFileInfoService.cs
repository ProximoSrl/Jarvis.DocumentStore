using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Jarvis.ImageService.Core.Services
{
    public interface IFileInfoService
    {
        FileInfo GetById(string id);
        void Save(FileInfo fileInfo);
        void Create(string id, string filename, SizeInfo[] getDefaultSizes);
        void LinkImage(string id, string size, string imageId);
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

        public void Create(string id, string filename, SizeInfo[] sizes)
        {
            var fi = new FileInfo(id, filename);
            foreach (var sizeInfo in sizes)
            {
                fi.LinkSize(sizeInfo.Name, null);
            }
            Save(fi);
            _scheduler.QueueThumbnail(id, sizes);
        }

        public void LinkImage(string id, string size, string imageId)
        {
            var fi = GetById(id);
            fi.LinkSize(size, imageId);
            Save(fi);
        }
    }
}

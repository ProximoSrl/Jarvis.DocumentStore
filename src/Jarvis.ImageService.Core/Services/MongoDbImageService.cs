using System.Net.Http;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Storage;
using MongoDB.Driver;

namespace Jarvis.ImageService.Core.Services
{
    public class MongoDbImageService : IImageService
    {
        readonly IPipelineScheduler _scheduler;
        readonly MongoCollection<ImageInfo> _collection;
        readonly IFileStore _fileStore;
        readonly ConfigService _config;

        public MongoDbImageService(
            MongoDatabase db, 
            IPipelineScheduler scheduler, 
            IFileStore fileStore, 
            ConfigService config
            )
        {
            _scheduler = scheduler;
            _fileStore = fileStore;
            _config = config;
            _collection = db.GetCollection<ImageInfo>("fileinfo");
        }

        ImageInfo GetById(string id)
        {
            return _collection.FindOneById(id.ToLowerInvariant());
        }

        void Save(ImageInfo imageInfo)
        {
            _collection.Save(imageInfo);
        }

        void Create(string id, string filename, ImageSizeInfo[] imageSizes)
        {
            var fi = new ImageInfo(id, filename);
            foreach (var sizeInfo in imageSizes)
            {
                fi.LinkSize(sizeInfo.Name, null);
            }
            Save(fi);
            _scheduler.QueueThumbnail(id, imageSizes);
        }

        public void LinkImage(string id, string size, string imageId)
        {
            var fi = GetById(id);
            fi.LinkSize(size, imageId);
            Save(fi);
        }

        public async Task<string> ReadFromHttp(HttpContent httpContent, string fileId)
        {
            if (httpContent == null || !httpContent.IsMimeMultipartContent())
            {
                return "Attachment not found!";
            }

            var provider = await httpContent.ReadAsMultipartAsync(
                new FileStoreMultipartStreamProvider(_fileStore, fileId)
                );

            if (provider.Filename == null)
            {
                return "Attachment not found!";
            }

            if (provider.UnsupportedExtension != null)
            {
                return string.Format("Unsupported file {0}", provider.UnsupportedExtension);
            }

            Create(
                fileId,
                provider.Filename,
                _config.GetDefaultSizes()
                );

            return null;
        }

        public IFileStoreDescriptor GetImageDescriptor(string fileId, string size)
        {
            var fileInfo = GetById(fileId);
            if (fileInfo == null)
            {
                return null;
            }

            size = size.ToLowerInvariant();

            if (!fileInfo.Sizes.ContainsKey(size))
            {
                return null;
            }

            return _fileStore.GetDescriptor(fileInfo.Sizes[size]);
        }
    }
}
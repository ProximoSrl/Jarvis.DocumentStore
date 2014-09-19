using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Storage;
using MongoDB.Bson;
using MongoDB.Driver;
using FileInfo = Jarvis.ImageService.Core.Model.FileInfo;

namespace Jarvis.ImageService.Core.Services
{
    public class MongoDbFileService : IFileService
    {
        readonly MongoCollection<FileInfo> _collection;
        readonly IFileStore _fileStore;
        readonly ConfigService _config;

        public MongoDbFileService(
            MongoDatabase db, 
            IFileStore fileStore, 
            ConfigService config
            )
        {
            _fileStore = fileStore;
            _config = config;
            _collection = db.GetCollection<FileInfo>("fileinfo");
        }

        public FileInfo GetById(FileId fileId)
        {
            return _collection.FindOneById((string)fileId);
        }

        void Save(FileInfo fileInfo)
        {
            _collection.Save(fileInfo);
        }

        void Create(FileId id, string filename, ImageSizeInfo[] imageSizes)
        {
            var fi = new FileInfo(id, filename);
            foreach (var sizeInfo in imageSizes)
            {
                fi.LinkSize(sizeInfo.Name, null);
            }
            Save(fi);
        }


        public void LinkImage(FileId fileId, string size, FileId imageId)
        {
            var fi = GetById(fileId);
            fi.LinkSize(size, imageId);
            Save(fi);
        }

        public async Task<string> UploadFromHttpContent(HttpContent httpContent, FileId fileId)
        {
            if (httpContent == null || !httpContent.IsMimeMultipartContent())
            {
                return "Attachment not found!";
            }

            var provider = await httpContent.ReadAsMultipartAsync(
                new FileStoreMultipartStreamProvider(_fileStore, fileId,_config)
            );

            if (provider.Filename == null)
            {
                return "Attachment not found!";
            }

            if (provider.IsInvalidFile)
            {
                return string.Format("Unsupported file {0}", provider.Filename);
            }

            Create(
                fileId,
                provider.Filename,
                _config.GetDefaultSizes()
            );

            return null;
        }

        public IFileStoreHandle GetImageDescriptor(FileId fileId, string size)
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
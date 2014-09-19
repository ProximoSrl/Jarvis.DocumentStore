using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.ImageService.Core.Storage
{
    public class GridFSFileStore : IFileStore
    {
        readonly MongoGridFS _gridFs;
        public ILogger Logger { get; set; }
        public GridFSFileStore(MongoDatabase db)
        {
            _gridFs = db.GetGridFS(MongoGridFSSettings.Defaults);
        }

        public Stream CreateNew(string fileId, string fname)
        {
            fileId = fileId.ToLowerInvariant();
            fname = fname.Replace("\"", "");

            Delete(fileId);
            return _gridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = fileId
            });
        }

        public IFileStoreDescriptor GetDescriptor(string fileId)
        {
            fileId = fileId.ToLowerInvariant();
            var s = _gridFs.FindOneById(fileId);
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", fileId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsFileStoreDescriptor(s);
        }

        public void Delete(string fileId)
        {
            fileId = fileId.ToLowerInvariant();
            _gridFs.DeleteById(fileId);
        }

        public string Download(string fileId, string folder)
        {
            fileId = fileId.ToLowerInvariant();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var s = _gridFs.FindOneById(fileId);
            var localFileName = Path.Combine(folder, s.Name);
            _gridFs.Download(localFileName,s);
            return localFileName;
        }

        public void Upload(string fileId, string pathToFile)
        {
            fileId = fileId.ToLowerInvariant();
            using (var inStream = File.OpenRead(pathToFile))
            {
                Upload(fileId, Path.GetFileName(pathToFile), inStream);
            }
        }

        public void Upload(string fileId, string fileName, Stream sourceStrem)
        {
            fileId = fileId.ToLowerInvariant();
            using (var outStream = CreateNew(fileId, fileName))
            {
                sourceStrem.CopyTo(outStream);
            }
        }
    }
}

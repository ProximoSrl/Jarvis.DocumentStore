using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.ImageService.Core.Storage
{
    public class GridFSFileStore : IFileStore
    {
        readonly MongoGridFS _gridFs;
        public GridFSFileStore(MongoDatabase db)
        {
            _gridFs = db.GetGridFS(MongoGridFSSettings.Defaults);
        }

        public Stream CreateNew(string fileId, string fname)
        {
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
            var s = _gridFs.FindOneById(fileId);
            return new GridFsFileStoreDescriptor(s);
        }

        public void Delete(string fileId)
        {
            _gridFs.DeleteById(fileId);
        }

        public string Download(string fileId, string folder)
        {
            var s = _gridFs.FindOneById(fileId);
            var localFileName = Path.Combine(folder, s.Name);
            _gridFs.Download(localFileName,s);
            return localFileName;
        }

        public void Upload(string fileId, string pathToFile)
        {
            using (var inStream = File.OpenRead(pathToFile))
            {
                using (var outStream = CreateNew(fileId, Path.GetFileName(pathToFile)))
                {
                    inStream.CopyTo(outStream);
                }
            }
        }
    }
}

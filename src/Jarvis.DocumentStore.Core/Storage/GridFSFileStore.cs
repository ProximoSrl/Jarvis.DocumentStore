using System;
using System.IO;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFSFileStore : IFileStore
    {
        readonly MongoGridFS _gridFs;
        public ILogger Logger { get; set; }
        public GridFSFileStore(MongoDatabase db)
        {
            _gridFs = db.GetGridFS(MongoGridFSSettings.Defaults);
        }

        public Stream CreateNew(FileId fileId, string fname)
        {
            fname = fname.Replace("\"", "");

            Delete(fileId);
            return _gridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)fileId
            });
        }

        public IFileStoreHandle GetDescriptor(FileId fileId)
        {
            var s = _gridFs.FindOneById((string)fileId);
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", fileId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsFileStoreHandle(s);
        }

        public void Delete(FileId fileId)
        {
            _gridFs.DeleteById((string)fileId);
        }

        public string Download(FileId fileId, string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var s = _gridFs.FindOneById((string)fileId);
            var localFileName = Path.Combine(folder, s.Name);
            _gridFs.Download(localFileName,s);
            return localFileName;
        }

        public void Upload(FileId fileId, string pathToFile)
        {
            Logger.DebugFormat("Uploading Id: {0} from file: {1}", fileId, pathToFile);
            using (var inStream = File.OpenRead(pathToFile))
            {
                Upload(fileId, Path.GetFileName(pathToFile), inStream);
            }
        }

        public void Upload(FileId fileId, string fileName, Stream sourceStrem)
        {
            Logger.DebugFormat("Uploading Id: {0} Name: {1}", fileId, fileName);
            using (var outStream = CreateNew(fileId, fileName))
            {
                sourceStrem.CopyTo(outStream);
            }
        }
    }
}

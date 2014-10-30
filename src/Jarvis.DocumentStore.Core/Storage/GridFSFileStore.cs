using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFsFileStoreWriter : IFileStoreWriter
    {
        public FileId FileId { get; private set; }
        public Stream WriteStream { get; private set; }

        public GridFsFileStoreWriter(FileId fileId, Stream writeStream)
        {
            FileId = fileId;
            WriteStream = writeStream;
        }
    }

    public class GridFSFileStore : IFileStore
    {
        private readonly MongoGridFS _fs;
        public ILogger Logger { get; set; }
        private ICounterService _counterService;

        public GridFSFileStore(MongoGridFS gridFs, ICounterService counterService)
        {
            _fs = gridFs;
            _counterService = counterService;
        }

        public IFileStoreWriter CreateNew(FileNameWithExtension fname)
        {
            var fileId = new FileId(_counterService.GetNext("file"));
            var stream = GridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)fileId
            });

            return new GridFsFileStoreWriter(fileId, stream);
        }

        public Stream CreateNew(FileId fileId, FileNameWithExtension fname)
        {
            Delete(fileId);
            return GridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)fileId
            });
        }

        public IFileStoreDescriptor GetDescriptor(FileId fileId)
        {
            var s = GridFs.FindOneById((string)fileId);
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", fileId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsFileStoreDescriptor(fileId, s);
        }

        public void Delete(FileId fileId)
        {
            GridFs.DeleteById((string)fileId);
        }

        public string Download(FileId fileId, string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var s = GridFs.FindOneById((string)fileId);
            var localFileName = Path.Combine(folder, s.Name);
            GridFs.Download(localFileName, s);
            return localFileName;
        }

        public void Upload(FileId fileId, string pathToFile)
        {
            Logger.DebugFormat("Uploading Id: {0} from file: {1}", fileId, pathToFile);
            using (var inStream = File.OpenRead(pathToFile))
            {
                Upload(fileId, new FileNameWithExtension(Path.GetFileName(pathToFile)), inStream);
            }
        }

        public void Upload(FileId fileId, FileNameWithExtension fileName, Stream sourceStrem)
        {
            Logger.DebugFormat("Uploading Id: {0} Name: {1}", fileId, fileName);
            using (var outStream = CreateNew(fileId, fileName))
            {
                sourceStrem.CopyTo(outStream);
            }
        }

        MongoGridFS GridFs
        {
            get{
                return _fs;
            }
        }
    }
}

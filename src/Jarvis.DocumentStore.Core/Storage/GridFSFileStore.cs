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
    public class FileStoreWriter : IFileStoreWriter
    {
        public FileId FileId { get; private set; }
        public Stream WriteStream { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public FileStoreWriter(FileId fileId, Stream writeStream, FileNameWithExtension fileName)
        {
            FileName = fileName;
            FileId = fileId;
            WriteStream = writeStream;
        }

        public void Dispose()
        {
            WriteStream.Dispose();
        }
    }

    public class GridFSFileStore : IFileStore
    {
        private readonly MongoGridFS _fs;
        public ILogger Logger { get; set; }
        private readonly ICounterService _counterService;

        public GridFSFileStore(MongoGridFS gridFs, ICounterService counterService)
        {
            _fs = gridFs;
            _counterService = counterService;
        }

        public IFileStoreWriter CreateNew(FileNameWithExtension fname)
        {
            var fileId = new FileId(_counterService.GetNext("file"));
            Logger.DebugFormat("Creating file {0} on {1}", fileId, GridFs.DatabaseName);
            var stream = GridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)fileId
            });

            return new FileStoreWriter(fileId, stream,fname);
        }

        public Stream CreateNew(FileId fileId, FileNameWithExtension fname)
        {
            Logger.DebugFormat("Creating file {0} on {1}", fileId, GridFs.DatabaseName);
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
            Logger.DebugFormat("GetDescriptor for file {0} on {1}", fileId, GridFs.DatabaseName);
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
            Logger.DebugFormat("Deleting file {0} on {1}", fileId, GridFs.DatabaseName);

            GridFs.DeleteById((string)fileId);
        }

        public string Download(FileId fileId, string folder)
        {
            Logger.DebugFormat("Downloading file {0} on {1} to folder {2}", fileId, GridFs.DatabaseName, folder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var s = GridFs.FindOneById((string)fileId);
            var localFileName = Path.Combine(folder, s.Name);
            GridFs.Download(localFileName, s);
            return localFileName;
        }

        public FileId Upload(string pathToFile)
        {
            using (var inStream = File.OpenRead(pathToFile))
            {
                return Upload(new FileNameWithExtension(Path.GetFileName(pathToFile)), inStream);
            }
        }

        public FileId Upload(FileNameWithExtension fileName, Stream sourceStrem)
        {
            using (var writer = CreateNew(fileName))
            {
                Logger.DebugFormat("Uploading file {0} named {1} on {2}", writer.FileId, fileName, GridFs.DatabaseName);
                sourceStrem.CopyTo(writer.WriteStream);
                return writer.FileId;
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

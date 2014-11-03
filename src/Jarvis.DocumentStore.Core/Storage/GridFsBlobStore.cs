using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFsBlobStore : IBlobStore
    {
        private readonly MongoGridFS _fs;
        public ILogger Logger { get; set; }
        private readonly ICounterService _counterService;

        public GridFsBlobStore(MongoGridFS gridFs, ICounterService counterService)
        {
            _fs = gridFs;
            _counterService = counterService;
        }

        public IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname)
        {
            var blobId = new BlobId(format, _counterService.GetNext(format));
            Logger.DebugFormat("Creating file {0} on {1}", blobId, GridFs.DatabaseName);
            var stream = GridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)blobId
            });

            return new BlobWriter(blobId, stream,fname);
        }

        public Stream CreateNew(BlobId blobId, FileNameWithExtension fname)
        {
            Logger.DebugFormat("Creating file {0} on {1}", blobId, GridFs.DatabaseName);
            Delete(blobId);
            return GridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)blobId
            });
        }

        public IFileStoreDescriptor GetDescriptor(BlobId blobId)
        {
            Logger.DebugFormat("GetDescriptor for file {0} on {1}", blobId, GridFs.DatabaseName);
            var s = GridFs.FindOneById((string)blobId);
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", blobId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsFileStoreDescriptor(blobId, s);
        }

        public void Delete(BlobId blobId)
        {
            Logger.DebugFormat("Deleting file {0} on {1}", blobId, GridFs.DatabaseName);

            GridFs.DeleteById((string)blobId);
        }

        public string Download(BlobId blobId, string folder)
        {
            Logger.DebugFormat("Downloading file {0} on {1} to folder {2}", blobId, GridFs.DatabaseName, folder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var s = GridFs.FindOneById((string)blobId);
            var localFileName = Path.Combine(folder, s.Name);
            GridFs.Download(localFileName, s);
            return localFileName;
        }

        public BlobId Upload(DocumentFormat format, string pathToFile)
        {
            using (var inStream = File.OpenRead(pathToFile))
            {
                return Upload(format, new FileNameWithExtension(Path.GetFileName(pathToFile)), inStream);
            }
        }

        public BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStrem)
        {
            using (var writer = CreateNew(format, fileName))
            {
                Logger.DebugFormat("Uploading file {0} named {1} on {2}", writer.BlobId, fileName, GridFs.DatabaseName);
                sourceStrem.CopyTo(writer.WriteStream);
                return writer.BlobId;
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

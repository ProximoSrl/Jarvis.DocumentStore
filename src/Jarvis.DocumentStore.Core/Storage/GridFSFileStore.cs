using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFSFileStore : IFileStore
    {
        private readonly ITenantAccessor _tenantAccessor;
        private MongoGridFS _fs;
        public ILogger Logger { get; set; }

        public GridFSFileStore(ITenantAccessor tenantAccessor)
        {
            _tenantAccessor = tenantAccessor;
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

        public IFileDescriptor GetDescriptor(FileId fileId)
        {
            var s = GridFs.FindOneById((string)fileId);
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", fileId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsFileDescriptor(fileId, s);
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
                return this._fs ?? (this._fs = _tenantAccessor.Current.Get<MongoGridFS>("grid.fs"));
            }
        }
    }
}

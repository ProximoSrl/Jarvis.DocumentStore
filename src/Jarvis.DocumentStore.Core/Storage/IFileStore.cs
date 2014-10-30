using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IFileStoreWriter
    {
        FileId FileId { get; }
        Stream WriteStream { get; }
    }

    public interface IFileStore
    {
        IFileStoreWriter CreateNew(FileNameWithExtension fname);
        Stream CreateNew(FileId fileId, FileNameWithExtension fname);
        IFileStoreDescriptor GetDescriptor(FileId fileId);
        void Delete(FileId fileId);
        string Download(FileId fileId, string folder);
        void Upload(FileId fileId, string pathToFile);
        void Upload(FileId fileId, FileNameWithExtension fileName, Stream sourceStrem);
    }
}
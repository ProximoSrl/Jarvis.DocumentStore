using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IFileStoreWriter : IDisposable
    {
        FileId FileId { get; }
        Stream WriteStream { get; }
        FileNameWithExtension FileName { get; }
    }

    public interface IFileStore
    {
        IFileStoreWriter CreateNew(FileNameWithExtension fname);
        IFileStoreDescriptor GetDescriptor(FileId fileId);
        void Delete(FileId fileId);
        string Download(FileId fileId, string folder);
        FileId Upload(string pathToFile);
        FileId Upload(FileNameWithExtension fileName, Stream sourceStrem);
    }
}
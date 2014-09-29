using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IFileStore
    {
        Stream CreateNew(FileId fileId, string fname);
        IFileStoreHandle GetDescriptor(FileId fileId);
        void Delete(FileId fileId);
        string Download(FileId fileId, string folder);
        void Upload(FileId fileId, string pathToFile);
        void Upload(FileId fileId, string fileName, Stream sourceStrem);
    }
}
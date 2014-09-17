using System.IO;

namespace Jarvis.ImageService.Core.Storage
{
    public interface IFileStore
    {
        Stream CreateNew(string fileId, string fname);
        IFileStoreDescriptor GetDescriptor(string fileId);
        void Delete(string fileId);
        string Download(string fileId, string folder);
        void Upload(string fileId, string pathToFile);
    }
}
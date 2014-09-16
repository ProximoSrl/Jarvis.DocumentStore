using System.IO;

namespace Jarvis.ImageService.Core.Storage
{
    public interface IFileStore
    {
        Stream CreateNew(string fileId, string fname);
        IFileStoreDescriptor GetDescriptor(string fileId);
        void Delete(string fileId);
    }
}
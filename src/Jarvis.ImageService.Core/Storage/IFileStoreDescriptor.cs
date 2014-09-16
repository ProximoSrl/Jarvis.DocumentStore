using System.IO;

namespace Jarvis.ImageService.Core.Storage
{
    public interface IFileStoreDescriptor
    {
        Stream OpenRead();
        string FileName { get; }
        string ContentType { get; }
    }
}
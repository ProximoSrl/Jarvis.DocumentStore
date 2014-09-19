using System.IO;

namespace Jarvis.ImageService.Core.Storage
{
    public interface IFileStoreHandle
    {
        Stream OpenRead();
        string FileName { get; }
        string ContentType { get; }
    }
}
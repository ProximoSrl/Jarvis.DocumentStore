using System.IO;

namespace Jarvis.ImageService.Core.Storage
{
    public interface IFileStoreReader
    {
        Stream OpenRead();
        string FileName { get; }
    }
}
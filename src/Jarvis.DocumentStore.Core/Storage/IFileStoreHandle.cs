using System.IO;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IFileStoreHandle
    {
        Stream OpenRead();
        string FileName { get; }
        string FileExtension { get; }
        string ContentType { get; }
    }
}
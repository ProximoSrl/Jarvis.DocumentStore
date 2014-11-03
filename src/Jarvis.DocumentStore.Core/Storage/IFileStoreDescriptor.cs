using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IFileStoreDescriptor
    {
        BlobId BlobId { get; }
        Stream OpenRead();
        FileNameWithExtension FileNameWithExtension { get; }
        string ContentType { get; }
        FileHash Hash { get; }
        long Length { get; }
    }
}
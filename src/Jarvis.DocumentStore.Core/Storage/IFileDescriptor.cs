using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IFileDescriptor
    {
        Stream OpenRead();
        string FileName { get; }
        string FileExtension { get; }
        string ContentType { get; }
        FileHash Hash { get; }
        long Length { get; }
    }
}
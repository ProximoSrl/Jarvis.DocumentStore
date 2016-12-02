using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// Interface that abstract the descriptor of the file.
    /// </summary>
    public interface IBlobDescriptor
    {
        BlobId BlobId { get; }
        Stream OpenRead();
        FileNameWithExtension FileNameWithExtension { get; }
        string ContentType { get; }

        /// <summary>
        /// MD5 has of the file.
        /// </summary>
        FileHash Hash { get; }

        /// <summary>
        /// Length of the file.
        /// </summary>
        long Length { get; }
    }
}
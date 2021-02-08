using System;
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

        /// <summary>
        /// Returns true if the binary blob exists, remember that this is a descriptor
        /// and the existence of this object does not guarantee that binary blob is ok
        /// </summary>
        Boolean Exists { get; }

        /// <summary>
        /// true if the descriptor point to a reference file, a file that is not store
        /// inside documenstore.
        /// </summary>
        Boolean IsReference { get; }
    }
}
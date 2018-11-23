using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// Sample interface to group all information needed to 
    /// write a stream into a <see cref="IBlobStore"/>
    /// </summary>
    public interface IBlobWriter : IDisposable
    {
        BlobId BlobId { get; }

        /// <summary>
        /// This is the stream that we can use to write data.
        /// </summary>
        Stream WriteStream { get; }
        FileNameWithExtension FileName { get; }
    }
}
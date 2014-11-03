using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IBlobWriter : IDisposable
    {
        BlobId BlobId { get; }
        Stream WriteStream { get; }
        FileNameWithExtension FileName { get; }
    }
}
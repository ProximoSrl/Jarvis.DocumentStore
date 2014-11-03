using System.IO;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public interface IBlobStore
    {
        IBlobDescriptor GetDescriptor(BlobId blobId);
        void Delete(BlobId blobId);
        string Download(BlobId blobId, string folder);

        IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname);
        BlobId Upload(DocumentFormat format, string pathToFile);
        BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStrem);
    }
}
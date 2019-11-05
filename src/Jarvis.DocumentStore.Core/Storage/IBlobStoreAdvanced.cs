using Jarvis.DocumentStore.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// This interface contains some more advanced functionalities
    /// for a <see cref="IBlobStore" /> that are needed for backup
    /// or for some specific functionalities.
    /// </summary>
    public interface IBlobStoreAdvanced : IBlobStore
    {
        /// <summary>
        /// Check if a blob exists, it is needed by the migrator tool.
        /// </summary>
        /// <param name="blobId"></param>
        /// <returns></returns>
        Boolean BlobExists(BlobId blobId);

        /// <summary>
        /// Raw persistence of a file Name
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="fileName"></param>
        IBlobDescriptor Persist(BlobId blobId, String fileName);

        /// <summary>
        /// Raw persistence of a stream
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="fileName"></param>
        /// <param name="inputStream"></param>
        IBlobDescriptor Persist(BlobId blobId, FileNameWithExtension fileName, Stream inputStream);

        /// <summary>
        /// Store raw from a blob descriptor, is used to migrate
        /// </summary>
        /// <param name="blobId">The blob id to store</param>
        /// <param name="descriptor">The original blob descriptor, all informatoin
        /// are used except the blob id that will be changed to <paramref name="blobId"/></param>
        void RawStore(BlobId blobId, IBlobDescriptor descriptor);
    }
}

using System;
using System.IO;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    /// <summary>
    /// This is the interface of a component that is capable to store
    /// blob to backup to a persisten store.
    /// </summary>
    internal interface IBlobArchiveStore
    {
        /// <summary>
        /// Store the blob inside the persistence engine
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileName"></param>
        /// <param name="blobId"></param>
        void Store(Stream stream, String fileName, String blobId);

        /// <summary>
        /// Delete a blob from the persistence engine.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="blobId"></param>
        void Delete(String fileName, String blobId);
    }
}

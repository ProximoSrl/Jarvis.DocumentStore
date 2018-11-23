using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    /// <summary>
    /// This is the interface of a component that is capable to store
    /// blob to backup to a persisten store.
    /// </summary>
    interface IBlobArchiveStore
    {
        /// <summary>
        /// Store the blob inside the persistence engine.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileName"></param>
        /// <param name="fileId"></param>
        void Store(Stream stream, String fileName, String fileId);

        /// <summary>
        /// Delete a blob from the persistence engine.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileId"></param>
        void Delete(String fileName, String fileId);
    }
}

using Castle.Core.Logging;
using System;
using System.IO;
using System.Text;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    /// <summary>
    /// A simple storage that allows to store blob archive to a standard
    /// directory. It is different from standard FSBlobStore because this
    /// will save files with the original names, to simplify retrieval.
    /// </summary>
    public class DirectoryBlobArchiveStore : IBlobArchiveStore
    {
        private readonly String _baseDirectory;
        private readonly String _baseDirectoryForDeleted;

        public ILogger Logger { get; set; }

        public DirectoryBlobArchiveStore(String baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _baseDirectoryForDeleted = Path.Combine(baseDirectory, "deleted");
            if (!Directory.Exists(_baseDirectoryForDeleted))
                Directory.CreateDirectory(_baseDirectoryForDeleted);

            Logger = NullLogger.Instance;
        }

        public void Store(Stream stream, string fileName, string blobId)
        {
            var finalFileName = GetFileNameFromBlobId(blobId, fileName, _baseDirectory);
            using (var destinationStream = new FileStream(finalFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                destinationStream.SetLength(0);
                stream.CopyTo(destinationStream);
            }
            Logger.Debug($"Blob {blobId} backed up to {finalFileName}");
        }

        public void Delete(string fileName, string blobId)
        {
            var finalFileName = GetFileNameFromBlobId(blobId, fileName, _baseDirectory);
            var deletedFileName = GetFileNameFromBlobId(blobId, fileName, _baseDirectoryForDeleted);
            if (File.Exists(finalFileName))
            {
                if (File.Exists(deletedFileName)) File.Delete(deletedFileName);
                File.Move(finalFileName, deletedFileName);
            }
            Logger.Debug($"Blob {blobId} deleted from backup file {finalFileName} and moved to file {deletedFileName}");
        }

        public String GetFileNameFromBlobId(String blobId, String fileName, String baseDirectory)
        {
            var splitted = blobId.Split('.');
            var id = Int64.Parse(splitted[1]);
            var stringPadded = String.Format("{0:D12}", id / 1000).Substring(0, 12);
            StringBuilder directoryName = new StringBuilder(16);
            for (int i = 0; i < stringPadded.Length; i++)
            {
                directoryName.Append(stringPadded[i]);
                if (i % 4 == 3) directoryName.Append(Path.DirectorySeparatorChar);
            }
            var finalDirectory = Path.Combine(baseDirectory, directoryName.ToString());
            if (!Directory.Exists(finalDirectory)) Directory.CreateDirectory(finalDirectory);

            return finalDirectory + id + "_" + fileName;
        }
    }
}

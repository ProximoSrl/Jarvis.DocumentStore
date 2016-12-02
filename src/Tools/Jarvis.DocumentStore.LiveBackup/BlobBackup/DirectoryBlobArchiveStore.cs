using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    /// <summary>
    /// A simple storage that allows to store blob archive to a standard
    /// directory.
    /// </summary>
    public class DirectoryBlobArchiveStore : IBlobArchiveStore
    {
        private readonly String _baseDirectory;
        private readonly String _baseDirectoryForDeleted;

        public DirectoryBlobArchiveStore(String baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _baseDirectoryForDeleted = Path.Combine(baseDirectory, "deleted");
            if (!Directory.Exists(_baseDirectoryForDeleted))
                Directory.CreateDirectory(_baseDirectoryForDeleted);
        }

        public void Store(Stream stream, string fileName, string fileId)
        {
            var finalFileName = GetFileNameFromFileId(fileId, fileName, _baseDirectory);
            using (var destinationStream = new FileStream(finalFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                destinationStream.SetLength(0);
                stream.CopyTo(destinationStream);
            }
        }

        public void Delete(string fileName, string fileId)
        {
            var finalFileName = GetFileNameFromFileId(fileId, fileName, _baseDirectory);
            if (File.Exists(finalFileName))
            {
                var deletedFileName = GetFileNameFromFileId(fileId, fileName, _baseDirectoryForDeleted);
                if (File.Exists(deletedFileName)) File.Delete(deletedFileName);
                File.Move(finalFileName, deletedFileName);
            }
        }

        public String GetFileNameFromFileId(String fileId, String fileName, String baseDirectory)
        {
            var splitted = fileId.Split('.');
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

            return finalDirectory  + id + "_" + fileName;
        }
    }
}

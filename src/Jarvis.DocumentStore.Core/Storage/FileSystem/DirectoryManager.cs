using Jarvis.DocumentStore.Core.Model;
using System;
using System.Text;
using Directory = Jarvis.DocumentStore.Shared.Helpers.DsDirectory;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    /// <summary>
    /// This class is needed to manage content of a directory to avoid
    /// cluttering all the files in a single folder.
    /// </summary>
    internal class DirectoryManager
    {
        public DirectoryManager(String baseDirectory)
        {
            BaseDirectory = baseDirectory;
            Directory.EnsureDirectory(baseDirectory);
        }

        public String BaseDirectory { get; private set; }

        /// <summary>
        /// Create a series of subdirectories that avoid cluttering thousands 
        /// of files inside the very same folder.
        /// The logic is the following, we want at most 1000 file in a folder, so
        /// we divide the id by 1000 and we pad to 15 number, then we subdivide
        /// the resulting number in blok by 4, each folder will contain at maximum 
        /// 1000 folders or files.
        /// </summary> 
        /// <param name="blobId"></param>
        /// <returns></returns>
        public String GetFileNameFromBlobId(BlobId blobId, String fileName)
        {
            return GetRawFileNameFromBlobId(blobId, fileName);
        }

        /// <summary>
        /// Create a series of subdirectories that avoid cluttering thousands 
        /// of files inside the very same folder.
        /// The logic is the following, we want at most 1000 file in a folder, so
        /// we divide the id by 1000 and we pad to 15 number, then we subdivide
        /// the resulting number in blok by 4, each folder will contain at maximum 
        /// 1000 folders or files.
        /// </summary> 
        /// <param name="blobId"></param>
        /// <returns></returns>
        public String GetDescriptorFileNameFromBlobId(BlobId blobId)
        {
            return GetRawFileNameFromBlobId(blobId, "descriptor");
        }

        private String GetRawFileNameFromBlobId(BlobId blobId, String fileName)
        {
            var id = blobId.Id;
            var stringPadded = String.Format("{0:D15}", id / 1000);
            StringBuilder directoryName = new StringBuilder(15);
            for (int i = 0; i < Math.Min(stringPadded.Length, 15); i++)
            {
                directoryName.Append(stringPadded[i]);
                if (i % 3 == 2) directoryName.Append(System.IO.Path.DirectorySeparatorChar);
            }
            var finalDirectory = Path.Combine(BaseDirectory, blobId.Format, directoryName.ToString());
            Directory.EnsureDirectory(finalDirectory);

            return finalDirectory + id + "." + Path.GetFileName(fileName);
        }
    }
}

using System.Net.Http;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Services
{
    /// <summary>
    /// File related operations service
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Associate an image file to a file thumbnail
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="size">Image size</param>
        /// <param name="imageId">Id of the image file</param>
        void LinkImage(FileId fileId, string size, FileId imageId);

        /// <summary>
        /// Get the file descriptor for the required thumbnail size
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="size">Thumnbail size</param>
        /// <returns>File description for the associated thumbnail</returns>
        IFileStoreHandle GetImageDescriptor(FileId fileId, string size);
        

        /// <summary>
        /// Get the file info
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <returns>the <see cref="T:Jarvis.DocumentStore.Core.Model.FileInfo"/> </returns>
        FileInfo GetById(FileId fileId);
    }
}

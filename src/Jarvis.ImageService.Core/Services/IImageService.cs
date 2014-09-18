using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Services
{
    public interface IImageService
    {
        void LinkImage(FileId id, string size, string imageId);
        Task<string> ReadFromHttp(HttpContent httpContent, FileId fileId);
        IFileStoreDescriptor GetImageDescriptor(FileId fileId, string size);
        ImageInfo GetById(FileId id);
    }
}

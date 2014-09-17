using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Services
{
    public interface IImageService
    {
        void LinkImage(string id, string size, string imageId);
        Task<string> ReadFromHttp(HttpContent httpContent, string fileId);
        IFileStoreDescriptor GetImageDescriptor(string fileId, string size);
    }
}

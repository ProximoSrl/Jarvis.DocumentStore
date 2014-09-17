using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ImageService.Core.Model
{
    public class ImageInfo
    {
        public ImageInfo(string id, string filename)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (filename == null) throw new ArgumentNullException("filename");

            Id = id.ToLowerInvariant();
            Filename = filename.Replace("\"", "");
            Sizes = new Dictionary<string, string>();
        }

        public void LinkSize(string size, string fileId)
        {
            Sizes[size.ToLowerInvariant()] = fileId;
        }

        public string Id { get; private set; }
        public string Filename { get; private set; }
        public IDictionary<string, string> Sizes { get; private set; }

        public string GetFileExtension()
        {
            return Path.GetExtension(Filename).ToLowerInvariant();
        }
    }
}

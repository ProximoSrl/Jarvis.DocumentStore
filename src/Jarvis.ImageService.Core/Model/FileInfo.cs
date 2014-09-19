using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Storage;
using MongoDB.Bson.Serialization;

namespace Jarvis.ImageService.Core.Model
{
    public class FileInfo 
    {
        public FileInfo(FileId id, string filename)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (filename == null) throw new ArgumentNullException("filename");

            Id = id;
            Filename = filename.Replace("\"", "");
            Sizes = new Dictionary<string, FileId>();
        }

        public void LinkSize(string size, FileId fileId)
        {
            Sizes[size.ToLowerInvariant()] = fileId;
        }

        public FileId Id { get; private set; }
        public string Filename { get; private set; }
        public IDictionary<string, FileId> Sizes { get; private set; }

        public string GetFileExtension()
        {
            return Path.GetExtension(Filename).ToLowerInvariant();
        }
    }
}

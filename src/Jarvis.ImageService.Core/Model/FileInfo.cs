using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ImageService.Core.Model
{
    public class FileInfo
    {
        public FileInfo(string id, string filename)
        {
            Id = id.ToLowerInvariant();
            Filename = filename;
        }

        public string Id { get; private set; }
        public string Filename { get; private set; }
    }
}

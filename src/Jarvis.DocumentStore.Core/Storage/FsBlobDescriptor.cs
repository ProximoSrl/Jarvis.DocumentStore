using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class FsBlobDescriptor : IBlobDescriptor
    {
        readonly string _pathToFile;

        public FsBlobDescriptor(BlobId id, string pathToFile)
        {
            _pathToFile = pathToFile;
            FileNameWithExtension = new FileNameWithExtension(Path.GetFileName(pathToFile));
            BlobId = id;
        }

        public BlobId BlobId { get; private set; }

        public Stream OpenRead()
        {
            return File.OpenRead(_pathToFile);
        }

        public FileNameWithExtension FileNameWithExtension { get; private set; }
 
        public string ContentType {
            get { return MimeTypes.GetMimeType(FileNameWithExtension); }
        }

        public FileHash Hash {
            get { throw new NotImplementedException(); }
        }

        public long Length {
            get { return new FileInfo(_pathToFile).Length; }
        }
    }
}

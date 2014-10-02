using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class FsFileDescriptor : IFileDescriptor
    {
        readonly string _pathToFile;

        public FsFileDescriptor(string pathToFile)
        {
            _pathToFile = pathToFile;
        }

        public Stream OpenRead()
        {
            return File.OpenRead(_pathToFile);
        }

        public string FileName
        {
            get { return Path.GetFileName(_pathToFile); }
        }
 
        public string FileExtension
        {
            get { return Path.GetExtension(_pathToFile); }
        }
        
        public string ContentType {
            get { return MimeTypes.GetMimeType(FileName); }
        }

        public FileHash Hash {
            get { throw new NotImplementedException(); }
        }

        public long Length {
            get { return new FileInfo(_pathToFile).Length; }
        }
    }
}

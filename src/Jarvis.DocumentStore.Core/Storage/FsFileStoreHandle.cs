using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class FsFileStoreHandle : IFileStoreHandle
    {
        readonly string _pathToFile;

        public FsFileStoreHandle(string pathToFile)
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
    }
}

using System;
using System.IO;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Model
{
    public class FileNameWithExtension
    {
        public string FileName { get; private set; }
        public string Extension { get; private set; }

        [JsonConstructor]
        public FileNameWithExtension(string fileName, string extension)
        {
            this.FileName = fileName;
            this.Extension = extension;
        }

        public FileNameWithExtension(string fileNameWithExtension)
        {
            if (String.IsNullOrWhiteSpace(fileNameWithExtension)) 
                throw new ArgumentNullException("fileNameWithExtension");

            fileNameWithExtension = fileNameWithExtension.Replace("\"", "");

            FileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);
            Extension = Path.GetExtension(fileNameWithExtension);

            if (!String.IsNullOrWhiteSpace(Extension))
                Extension = Extension.Remove(0, 1).ToLowerInvariant();
        }

        public static implicit operator string(FileNameWithExtension fname)
        {
            return Path.ChangeExtension(fname.FileName, fname.Extension);
        }

        public override string ToString()
        {
            return (string) this;
        }
    }
}
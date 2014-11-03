using System;
using System.IO;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Model
{
    public class FileNameWithExtension : IEquatable<FileNameWithExtension>
    {
        public bool Equals(FileNameWithExtension other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(FileName, other.FileName) && string.Equals(Extension, other.Extension);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileNameWithExtension) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FileName != null ? FileName.GetHashCode() : 0)*397) ^ (Extension != null ? Extension.GetHashCode() : 0);
            }
        }

        public static bool operator ==(FileNameWithExtension left, FileNameWithExtension right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FileNameWithExtension left, FileNameWithExtension right)
        {
            return !Equals(left, right);
        }

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
            if (Path.HasExtension(fname.FileName))
                return Path.ChangeExtension(fname.FileName+".", fname.Extension);
            
            return Path.ChangeExtension(fname.FileName, fname.Extension);
        }

        public override string ToString()
        {
            return (string) this;
        }
    }
}
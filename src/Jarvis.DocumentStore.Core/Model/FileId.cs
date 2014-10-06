using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Internal file handle
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<FileId>))]
    public class FileId : LowercaseStringValue
    {
        public FileId(string value) : base(value)
        {
            //if (value == null) 
            //    throw new ArgumentNullException("value");
        }
    }

    /// <summary>
    /// Public file handle
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<FileAlias>))]
    public class FileAlias : LowercaseStringValue
    {
        public FileAlias(string value) : base(value)
        {
        }
    }

    /// <summary>
    /// Public file handle
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<FileHash>))]
    public class FileHash : LowercaseStringValue
    {
        public FileHash(string value)
            : base(value)
        {
        }
    }

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
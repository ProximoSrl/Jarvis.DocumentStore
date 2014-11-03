using System.Collections.Generic;
using System.ComponentModel;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Internal file handle
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<FileId>))]
    public class FileId : LowercaseStringValue
    {
        public FileId(long value) : base("File_"+value.ToString())
        {
        }

        public FileId(string value) : base(value)
        {
            //if (value == null) 
            //    throw new ArgumentNullException("value");
        }

        public static readonly FileId Null = new FileId("null");
    }
}
using System;
using System.ComponentModel;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Internal file handle
    /// </summary>
    [BsonSerializer(typeof(TypedStringValueBsonSerializer<BlobId>))]
    [TypeConverter(typeof(StringValueTypeConverter<BlobId>))]
    public class BlobId : LowercaseStringValue
    {
        public BlobId(DocumentFormat format, long value)
            : base(format +"." + value)
        {
        }

        [BsonIgnore]
        [JsonIgnore]
        public DocumentFormat Format {
            get
            {
                var dotPos =  Value.LastIndexOf('.');
                if (dotPos <= 0)
                    throw new Exception("Invalid BlobId, missing format!");

                return new DocumentFormat(Value.Substring(0, dotPos));
            } 
        }

        public BlobId(string value) : base(value)
        {
            //if (value == null) 
            //    throw new ArgumentNullException("value");
        }

        public static readonly BlobId Null = new BlobId("null");
    }
}
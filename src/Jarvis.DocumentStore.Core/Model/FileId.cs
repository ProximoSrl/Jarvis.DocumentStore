using System;
using System.Collections.Generic;
using System.ComponentModel;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.IdentitySupport;
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
}
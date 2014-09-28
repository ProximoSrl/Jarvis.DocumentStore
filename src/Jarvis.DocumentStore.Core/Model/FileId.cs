using System;
using System.Collections.Generic;
using System.ComponentModel;
using CQRS.Shared.Domain;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    [BsonSerializer(typeof(StringValueSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<FileId>))]
    public class FileId : LowercaseStringValue
    {
        public FileId(string value) : base(value)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Http;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    [BsonSerializer(typeof(FileIdSerializer))]
    [TypeConverter(typeof(FileIdTypeConverter))]
    public class FileId : LowercaseStringId
    {
        public FileId(string id) : base(id)
        {
        }
    }
}
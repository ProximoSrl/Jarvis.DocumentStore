using System.ComponentModel;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Public file handle
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<FileHandle>))]
    public class FileHandle : LowercaseStringValue
    {
        public FileHandle(string value) : base(value)
        {
        }
    }
}
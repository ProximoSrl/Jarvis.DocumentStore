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
    [TypeConverter(typeof(StringValueTypeConverter<FileHash>))]
    public class FileHash : LowercaseStringValue
    {
        public FileHash(string value)
            : base(value)
        {
        }
    }
}
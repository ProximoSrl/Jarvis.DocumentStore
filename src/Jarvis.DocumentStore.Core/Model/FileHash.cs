using System.ComponentModel;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Public file handle
    /// </summary>
    [BsonSerializer(typeof(TypedStringValueBsonSerializer<FileHash>))]
    [TypeConverter(typeof(StringValueTypeConverter<FileHash>))]
    public class FileHash : LowercaseStringValue
    {
        public FileHash(string value)
            : base(value)
        {
        }
    }
}
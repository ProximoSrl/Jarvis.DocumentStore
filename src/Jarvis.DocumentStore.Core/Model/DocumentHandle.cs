using System.ComponentModel;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Public document handle
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<DocumentHandle>))]
    public class DocumentHandle : LowercaseStringValue
    {
        public DocumentHandle(string value) : base(value)
        {
        }
    }
}
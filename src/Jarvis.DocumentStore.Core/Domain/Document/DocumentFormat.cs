using System.ComponentModel;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<DocumentFormat>))]
    public class DocumentFormat : LowercaseStringValue
    {
        public DocumentFormat(string value)
            : base(value)
        {
        }
    }
}
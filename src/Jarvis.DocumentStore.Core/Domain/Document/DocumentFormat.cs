using System.ComponentModel;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
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
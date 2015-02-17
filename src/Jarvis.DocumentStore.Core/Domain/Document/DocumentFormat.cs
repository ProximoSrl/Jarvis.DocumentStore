using System.ComponentModel;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    /// <summary>
    /// We have an identical class named <see cref="Client.Model.DocumentFormat"/> that is duplicated
    /// because it does not depends on BsonSerializer attribute. We want to minimize client dependency to 
    /// third party library
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<DocumentFormat>))]
    public class DocumentFormat : LowercaseStringValue
    {
        public DocumentFormat(string value)
            : base(value)
        {
        }

        public static implicit operator DocumentFormat(Jarvis.DocumentStore.Client.Model.DocumentFormat clientDocumentFormat)
        {
            return new DocumentFormat(clientDocumentFormat);
        }
    }

    
}
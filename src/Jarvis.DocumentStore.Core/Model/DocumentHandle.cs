using System.ComponentModel;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// Public document handle
    /// </summary>
    [BsonSerializer(typeof(TypedStringValueBsonSerializer<DocumentHandle>))]
    [TypeConverter(typeof(StringValueTypeConverter<DocumentHandle>))]
    public class DocumentHandle : LowercaseStringValue
    {
        public static readonly DocumentHandle Empty = new DocumentHandle(string.Empty);
        
        public DocumentHandle(string value) : base(value)
        {
        }
    }
}
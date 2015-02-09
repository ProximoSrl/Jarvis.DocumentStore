using System.ComponentModel;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    /// <summary>
    /// PipelineId
    /// </summary>
    [BsonSerializer(typeof(StringValueBsonSerializer))]
    [TypeConverter(typeof(StringValueTypeConverter<PipelineId>))]
    public class PipelineId : LowercaseStringValue
    {
        public PipelineId(string value)
            : base(value)
        {
        }

        public static readonly PipelineId Null = new PipelineId("null");
    }
}
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using log4net.Util.TypeConverters;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Model
{
    [BsonSerializer(typeof(TypedStringValueBsonSerializer<QueuedJobId>))]
    [TypeConverter(typeof(StringValueTypeConverter<QueuedJobId>))]
    public class QueuedJobId : LowercaseStringValue
    {
        public QueuedJobId(string value)
            : base(value)
        {
        }
    }
}

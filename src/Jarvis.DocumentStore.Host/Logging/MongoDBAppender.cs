using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using MongoDB.Bson;

namespace Jarvis.DocumentStore.Host.Logging
{
    public class MongoAppenderFileld
    {
        public string Name { get; set; }
        public IRawLayout Layout { get; set; }
    }

    public class MongoDBAppender : AppenderSkeleton
    {
        private readonly List<MongoAppenderFileld> _fields = new List<MongoAppenderFileld>();
        
        protected override void Append(LoggingEvent loggingEvent)
        {
            
        }

        public void AddField(MongoAppenderFileld fileld)
        {
            _fields.Add(fileld);
        }

        private BsonDocument BuildBsonDocument(LoggingEvent log)
        {
            if (_fields.Count == 0)
                throw new Exception("invalid log configuration");

            var doc = new BsonDocument();
            foreach (MongoAppenderFileld field in _fields)
            {
                object value = field.Layout.Format(log);
                var bsonValue = value as BsonValue ?? BsonValue.Create(value);
                doc.Add(field.Name, bsonValue);
            }
            return doc;
        }
    }
}

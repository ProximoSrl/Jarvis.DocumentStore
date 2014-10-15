using System;
using System.Collections;
using System.Linq;
using log4net.Core;
using log4net.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Host.Logging
{
    public class MongoLog
    {
        public class TimeToLive
        {
            public int Days { get; set; }
            public int Hours { get; set; }
            public int Minutes { get; set; }

            public TimeSpan ToTimeSpan()
            {
                return new TimeSpan(Days,Hours,Minutes,0);
            }
        }

        string _machineName;
        MongoCollection<BsonDocument> _logCollection;

        public string MachineName
        {
            get
            {
                if (_machineName == null)
                    _machineName = Environment.MachineName;

                return _machineName;
            }
            private set { _machineName = value; }
        }



        public void SetupCollection()
        {
            var uri = new MongoUrl(ConnectionString);
            var client = new MongoClient(uri);
            MongoDatabase db = client.GetServer().GetDatabase(uri.DatabaseName);

            _logCollection = db.GetCollection(CollectionName);
            var builder = new IndexOptionsBuilder();

            if (ExpireAfter != null)
            {
                builder.SetTimeToLive(ExpireAfter.ToTimeSpan());
            }

            _logCollection.CreateIndex(IndexKeys.Descending(FieldNames.Timestamp), builder);
            _logCollection.CreateIndex(IndexKeys
                .Ascending(FieldNames.Level, FieldNames.Thread, FieldNames.Loggername)
            );
        }

        BsonDocument LoggingEventToBSON(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) return null;

            var toReturn = new BsonDocument();
            toReturn[FieldNames.Timestamp] = loggingEvent.TimeStamp;
            toReturn[FieldNames.Level] = loggingEvent.Level.ToString();
            toReturn[FieldNames.Thread] = loggingEvent.ThreadName ?? String.Empty;
            toReturn[FieldNames.Username] = loggingEvent.UserName ?? String.Empty;
            toReturn[FieldNames.Message] = loggingEvent.RenderedMessage;
            toReturn[FieldNames.Loggername] = loggingEvent.LoggerName ?? String.Empty;
            toReturn[FieldNames.Domain] = loggingEvent.Domain ?? String.Empty;
            toReturn[FieldNames.Machinename] = MachineName ?? String.Empty;

            // location information, if available
            if (loggingEvent.LocationInformation != null)
            {
                toReturn[FieldNames.Filename] = loggingEvent.LocationInformation.FileName ?? String.Empty;
                toReturn[FieldNames.Method] = loggingEvent.LocationInformation.MethodName ?? String.Empty;
                toReturn[FieldNames.Linenumber] = loggingEvent.LocationInformation.LineNumber ?? String.Empty;
                toReturn[FieldNames.Classname] = loggingEvent.LocationInformation.ClassName ?? String.Empty;
            }

            // exception information
            if (loggingEvent.ExceptionObject != null)
            {
                toReturn[FieldNames.Exception] = ExceptionToBSON(loggingEvent.ExceptionObject);
            }

            // properties
            PropertiesDictionary compositeProperties = loggingEvent.GetProperties();
            if (compositeProperties != null && compositeProperties.Count > 0)
            {
                var properties = new BsonDocument();
                foreach (DictionaryEntry entry in compositeProperties)
                {
                    if (entry.Value == null) continue;

                    //remember that no property can have a point in it because it cannot be saved.
                    String key = entry.Key.ToString().Replace(".", "|");
                    BsonValue value;
                    if (!BsonTypeMapper.TryMapToBsonValue(entry.Value, out value))
                    {
                        properties[key] = entry.Value.ToBsonDocument();
                    }
                    else
                    {
                        properties[key] = value;
                    }
                }
                toReturn[FieldNames.Customproperties] = properties;
            }

            return toReturn;
        }

        /// <summary>
        ///     Create BSON representation of Exception
        ///     Inner exceptions are handled recursively
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected BsonDocument ExceptionToBSON(Exception ex)
        {
            var toReturn = new BsonDocument();
            toReturn[FieldNames.Message] = ex.Message;
            toReturn[FieldNames.Source] = ex.Source ?? string.Empty;
            toReturn[FieldNames.Stacktrace] = ex.StackTrace ?? string.Empty;

            if (ex.InnerException != null)
            {
                toReturn[FieldNames.Innerexception] = ExceptionToBSON(ex.InnerException);
            }

            return toReturn;
        }

        public string ConnectionString { get; set; }
        public string CollectionName { get; set; }
        public TimeToLive ExpireAfter { get; set; }

        public void InsertBatch(LoggingEvent[] events)
        {
            if (_logCollection!= null)
            {
                var docs = events.Select(LoggingEventToBSON).Where(x => x != null).ToArray();
                _logCollection.InsertBatch(docs);
            }
        }

        public void Insert(LoggingEvent loggingEvent)
        {
            if (_logCollection != null)
            {
                BsonDocument doc = LoggingEventToBSON(loggingEvent);
                if (doc != null)
                {
                    _logCollection.Insert(doc);
                }
            }
        }
    }
}
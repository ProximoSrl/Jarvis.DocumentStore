using System;
using System.Collections;
using System.Linq;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Host.Logging
{
    public  class BufferedMongoDBAppender : BufferingAppenderSkeleton
    {
        const string DefaultConnectionString = "mongodb://localhost/log4net";
        const string DefaultCollectionName = "logs";
        const Int32 DefaultCappedSize = 500 * 1000 * 1000;

        string _machineName;

        Int32 _cappedSize = DefaultCappedSize;

        string _collectionName = DefaultCollectionName;
        string _connectionString = DefaultConnectionString;

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

        protected override bool RequiresLayout
        {
            get { return false; }
        }

        /// <summary>
        ///     Mongo collection used for logs
        ///     The main reason of exposing this is to have same log collection available for unit tests
        /// </summary>
        public MongoCollection LogCollection { get; private set; }

        #region Appender configuration properties

        /// <summary>
        ///     Hostname of MongoDB server
        ///     Defaults to DEFAULT_MONGO_HOST
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        /// <summary>
        ///     Hostname of MongoDB server
        ///     Defaults to DEFAULT_MONGO_HOST
        /// </summary>
        public Int32 CappedSize
        {
            get { return _cappedSize; }
            set { _cappedSize = value; }
        }

        /// <summary>
        ///     Name of the collection in database
        ///     Defaults to DEFAULT_COLLECTION_NAME
        /// </summary>
        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = value; }
        }

        #endregion

        public override void ActivateOptions()
        {
            try
            {
                var uri = new MongoUrl(ConnectionString);
                var client = new MongoClient(uri);
                MongoDatabase db = client.GetServer().GetDatabase(uri.DatabaseName);

                if (!db.CollectionExists(_collectionName))
                {
                    CollectionOptionsBuilder options = CollectionOptions.SetCapped(true).SetMaxSize(CappedSize);
                    db.CreateCollection(_collectionName, options);
                }

                LogCollection = db.GetCollection(CollectionName);

                LogCollection.CreateIndex(IndexKeys
                    .Descending(FieldNames.Timestamp)
                    .Ascending(FieldNames.Level, FieldNames.Thread, FieldNames.Loggername)
                    );

                //                BsonSerializer.RegisterSerializationProvider(new LoggerBsonSerializerProvider());
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Exception while initializing MongoDB Appender", e, ErrorCode.GenericFailure);
            }
        }

        protected override void OnClose()
        {
            LogCollection = null;
            base.OnClose();
        }

        /// <summary>
        ///     Create BSON representation of LoggingEvent
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <returns></returns>
        protected BsonDocument LoggingEventToBSON(LoggingEvent loggingEvent)
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

        protected override void SendBuffer(LoggingEvent[] events)
        {
            if (LogCollection != null)
            {
                var docs = events.Select(LoggingEventToBSON).Where(x=> x != null).ToArray();
                LogCollection.InsertBatch(docs);
            }
        }
    }
}
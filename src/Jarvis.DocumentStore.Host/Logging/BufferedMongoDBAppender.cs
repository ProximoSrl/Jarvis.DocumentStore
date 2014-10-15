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
    public class BufferedMongoDBAppender : BufferingAppenderSkeleton
    {
        public MongoLog Settings { get; set; }

        protected override bool RequiresLayout
        {
            get { return false; }
        }
        
        public override void ActivateOptions()
        {
            try
            {
                Settings.SetupCollection();
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Exception while initializing MongoDB Appender", e, ErrorCode.GenericFailure);
            }
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            Settings.InsertBatch(events);
        }
    }
}
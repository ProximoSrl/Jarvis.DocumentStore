﻿using System;
using System.Collections;
using log4net.Appender;
using log4net.Core;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Logging
{
    public interface IMongoAppenderCollectionProvider
    {
        MongoCollection GetCollection();
    }

    public class MongoDBAppender : AppenderSkeleton, IMongoAppenderCollectionProvider
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

        protected override void Append(LoggingEvent loggingEvent)
        {
            Settings.Insert(loggingEvent);
        }

        public MongoCollection GetCollection()
        {
            return Settings.LogCollection;
        }
    }
}
using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Logging
{
    public class BufferedElasticSearchAppender : BufferingAppenderSkeleton
    {
        public ElasticSearchLog Settings { get; set; }

        protected override bool RequiresLayout
        {
            get { return false; }
        }

        public override void ActivateOptions()
        {
            try
            {
                Settings.Initialize();
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Exception while initializing elasticSearch Appender", e, ErrorCode.GenericFailure);
            }
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            Settings.InsertBatch(events);
        }

    }
}

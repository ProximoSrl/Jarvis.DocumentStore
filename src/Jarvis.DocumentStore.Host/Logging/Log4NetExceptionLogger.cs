using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Host.Support;

namespace Jarvis.DocumentStore.Host.Logging
{
    public class Log4NetExceptionLogger : ExceptionLogger
    {
        private readonly ILoggerFactory _loggerFactory;

        public Log4NetExceptionLogger(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override void Log(ExceptionLoggerContext context)
        {
            var type = typeof(DocumentStoreApplication);
            if (context.ExceptionContext.ControllerContext != null)
            {
                type = context.ExceptionContext.ControllerContext.Controller.GetType();
            }
            var logger = _loggerFactory.Create(type);
            logger.ErrorFormat(context.Exception, "* * * * * * * * * * * *");
        }
    }
}

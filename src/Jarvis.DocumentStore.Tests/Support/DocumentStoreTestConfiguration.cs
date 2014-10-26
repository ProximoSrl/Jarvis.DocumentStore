using System;
using System.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;

namespace Jarvis.DocumentStore.Tests.Support
{
    public class DocumentStoreTestConfiguration : DocumentStoreConfiguration
    {
        public DocumentStoreTestConfiguration()
        {
            IsApiServer = true;
            IsWorker = false;
            IsReadmodelBuilder = true;

            QuartzConnectionString = ConfigurationManager.ConnectionStrings["ds.quartz"].ConnectionString;

            TenantSettings.Add(new TestTenantSettings());
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
        }
    }

    internal class ExtendedConsoleLogger : ConsoleLogger, IExtendedLogger
    {
        public ExtendedConsoleLogger(string name) : base(name)
        {
            
        }
        public ExtendedConsoleLogger(string name, LoggerLevel loggerLevel)
            :base(name,loggerLevel)
        {
            
        }

        public IContextProperties GlobalProperties { get; private set; }
        public IContextProperties ThreadProperties { get; private set; }
        public IContextStacks ThreadStacks { get; private set; }
    }

    internal class ExtendedConsoleLoggerFactory : ConsoleFactory, IExtendedLoggerFactory
    {
        public ExtendedConsoleLoggerFactory()
        {
            
        }

        public IExtendedLogger Create(Type type)
        {
            return new ExtendedConsoleLogger(type.Name);
        }

        public IExtendedLogger Create(string name)
        {
            return new ExtendedConsoleLogger(name);
        }

        public IExtendedLogger Create(Type type, LoggerLevel level)
        {
            return new ExtendedConsoleLogger(type.Name, level);
        }

        public IExtendedLogger Create(string name, LoggerLevel level)
        {
            return new ExtendedConsoleLogger(name, level);
        }
    }
}
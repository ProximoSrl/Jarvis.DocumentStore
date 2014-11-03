using System;
using Castle.Core.Logging;

namespace Jarvis.DocumentStore.Tests.Support
{
    internal class ExtendedConsoleLoggerFactory : ConsoleFactory, IExtendedLoggerFactory
    {
        public ExtendedConsoleLoggerFactory()
        {
            
        }

        public new IExtendedLogger Create(Type type)
        {
            return new ExtendedConsoleLogger(type.Name);
        }

        public new IExtendedLogger Create(string name)
        {
            return new ExtendedConsoleLogger(name);
        }

        public new IExtendedLogger Create(Type type, LoggerLevel level)
        {
            return new ExtendedConsoleLogger(type.Name, level);
        }

        public new IExtendedLogger Create(string name, LoggerLevel level)
        {
            return new ExtendedConsoleLogger(name, level);
        }
    }
}
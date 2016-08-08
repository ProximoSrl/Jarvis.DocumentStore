using System;
using Castle.Core.Logging;

namespace Jarvis.DocumentStore.Tests.Support
{
    internal class ExtendedConsoleLoggerFactory : ConsoleFactory, IExtendedLoggerFactory
    {
        internal static LoggerLevel DefaultLoggerLevel { get; set; }

        static ExtendedConsoleLoggerFactory()
        {
            DefaultLoggerLevel = LoggerLevel.Warn;
        }

        public ExtendedConsoleLoggerFactory()
        {
            
        }

        public new IExtendedLogger Create(Type type)
        {
            return new ExtendedConsoleLogger(type.Name, DefaultLoggerLevel);
        }

        public new IExtendedLogger Create(string name)
        {
            return new ExtendedConsoleLogger(name, DefaultLoggerLevel);
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
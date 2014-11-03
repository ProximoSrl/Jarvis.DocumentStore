using Castle.Core.Logging;

namespace Jarvis.DocumentStore.Tests.Support
{
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
}
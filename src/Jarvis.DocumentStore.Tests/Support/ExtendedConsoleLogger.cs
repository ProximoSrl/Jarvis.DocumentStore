using Castle.Core.Logging;
using Jarvis.Framework.TestHelpers;

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

        private TestLogger.TestThreadProperties _threadProperties = new TestLogger.TestThreadProperties();
        private TestLogger.TestGlobalProperties _globalProperties = new TestLogger.TestGlobalProperties();
        private TestLogger.TestContextStacks _contextStacks = new TestLogger.TestContextStacks();
        public IContextProperties GlobalProperties
        {
            get { return _globalProperties; }
        }

        public IContextProperties ThreadProperties
        {
            get { return _threadProperties; }
        }

        public IContextStacks ThreadStacks
        {
            get { return _contextStacks; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture]
    public class TikaAnalyzerTests
    {
        [Test]
        public void html_should_have_pages()
        {
            var task = new TikaAnalyzer(new ConfigService())
            {
                Logger = new ConsoleLogger()
            };

            task.Run(TestConfig.PathToMultipageWordDocument, test =>
            {
                Debug.WriteLine(test);
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.BackgroundTasks;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.BackgroudTasksTests
{
    [TestFixture]
    public class ImportFromFileTests
    {
        private ImportFormatFromFileQueue _queue;

        [SetUp]
        public void SetUp()
        {
            _queue = new ImportFormatFromFileQueue(new string[] { TestConfig.QueueFolder })
            {
                Logger = new ConsoleLogger()
            };
        }

        [Test]
        public void should_load_task()
        {
            var pathToTask = Path.Combine(TestConfig.QueueFolder, "File_1.dsimport");
            var pathToFile = new Uri(TestConfig.PathToWordDocument);
            var descriptor = _queue.LoadTask(pathToTask);

            Assert.NotNull(descriptor);
            Assert.AreEqual(pathToFile, descriptor.Uri);
        }

        [Test]
        public void poll()
        {
            _queue.PollFileSystem();
        }
    }
}
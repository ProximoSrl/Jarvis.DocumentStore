using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.BackgroundTasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.Framework.Shared.MultitenantSupport;
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
            var descriptor = _queue.LoadTask(pathToTask);

            Assert.NotNull(descriptor);
            Assert.AreEqual(new Uri(TestConfig.PathToWordDocument), descriptor.Uri);
            Assert.AreEqual(new DocumentFormat("original"), descriptor.Format);
            Assert.AreEqual(new DocumentHandle("word"), descriptor.Handle);
            Assert.AreEqual(new TenantId("docs"), descriptor.Tenant);
            
            Assert.NotNull(descriptor.CustomData);
            Assert.AreEqual("2050-01-01", descriptor.CustomData["expire-on"]);
        }

        [Test]
        public void poll()
        {
            _queue.PollFileSystem();
        }
    }
}
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Tests.Support;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using CQRS.Shared.MultitenantSupport;

namespace Jarvis.DocumentStore.Tests.JobTests.Queue
{
    [TestFixture]
    [Category("current")]
    public class QueueHandlerTests
    {
        MongoDatabase _db = MongoDbTestConnectionProvider.ReadModelDb;

        [SetUp]
        public void SetUp() 
        {
            _db.Drop();
        }

        [Test]
        public void verify_file_extension_on_handler_filter_exact_extension()
        {
            var info = new QueueInfo("test", "", "pdf|doc" ) ;
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx")
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue-test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(0));

        }

        [Test]
        public void verify_file_extension_permitted()
        {
            var info = new QueueInfo("test", "", "pdf|docx");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx")
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue-test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1));

        }
    }
}

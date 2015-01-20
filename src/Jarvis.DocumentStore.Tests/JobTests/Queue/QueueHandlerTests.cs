using Jarvis.DocumentStore.Core.Jobs.QueueManager;
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
        public void verify_regex_on_handler()
        {
            var info = new QueueInfo() 
            {
                Name = "test",
                Extension = "pdf|doc"
            };
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                
            };
            sut.Handle(rm);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.IdentitySupport.Serialization;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class HandleProjectionTests
    {
        HandleWriter _writer;
        readonly DocumentHandle _documentHandle = new DocumentHandle("a");
        readonly DocumentId Document_1 = new DocumentId(1);
        readonly DocumentId Document_2 = new DocumentId(2);

        [SetUp]
        public void SetUp()
        {
            MongoDbTestConnectionProvider.ReadModelDb.Drop();

            var mngr = new IdentityManager(new CounterService(MongoDbTestConnectionProvider.ReadModelDb));
            mngr.RegisterIdentitiesFromAssembly(typeof(DocumentId).Assembly);

            EventStoreIdentityBsonSerializer.IdentityConverter = mngr;

            EventStoreIdentityCustomBsonTypeMapper.Register<DocumentId>();
            EventStoreIdentityCustomBsonTypeMapper.Register<HandleId>();
            StringValueCustomBsonTypeMapper.Register<BlobId>();
            StringValueCustomBsonTypeMapper.Register<DocumentHandle>();
            StringValueCustomBsonTypeMapper.Register<FileHash>();

            _writer = new HandleWriter(MongoDbTestConnectionProvider.ReadModelDb);
        }

        [Test]
        public void Promise()
        {
            _writer.Promise(_documentHandle, Document_1, 1);

            var h = _writer.Get(_documentHandle);
            Assert.NotNull(h);
            Assert.AreEqual(Document_1, h.DocumentId);
            Assert.AreEqual(0, h.ProjectedAt);
            Assert.AreEqual(1, h.CreatetAt);
        }

        [Test]
        public void create()
        {
            _writer.Create(_documentHandle);
            var h = _writer.Get(_documentHandle);

            Assert.NotNull(h);
            Assert.IsNull(h.DocumentId);
            Assert.AreEqual(0, h.ProjectedAt);
            Assert.AreEqual(0, h.CreatetAt);
        }

        [Test]
        public void update_custom_data()
        {
            _writer.Create(_documentHandle);
            var handleCustomData = new HandleCustomData() { { "a", "b" } };
            _writer.UpdateCustomData(_documentHandle, handleCustomData);
            var h = _writer.Get(_documentHandle);

            Assert.NotNull(h.CustomData);
            Assert.AreEqual("b", (string)h.CustomData["a"]);
        }

        [Test]
        [TestCase(1, 2, 10, false)]
        [TestCase(1, 2, 11, false)]
        [TestCase(1, 1, 9, true)]
        public void Projected(int promisedDocId, int expectedDocId, int projectedAt, bool isPending)
        {
            var promisedDocumentId = new DocumentId(promisedDocId);
            var expectedDocumentId = new DocumentId(expectedDocId);
            _writer.Promise(_documentHandle, promisedDocumentId, 10);

            _writer.ConfirmLink(_documentHandle, Document_2, projectedAt);

            var h = _writer.Get(_documentHandle);
            Assert.NotNull(h);
            Assert.AreEqual(expectedDocumentId, h.DocumentId);
            Assert.AreEqual(10, h.CreatetAt);

            if (h.ProjectedAt >= h.CreatetAt)
                Assert.AreEqual(projectedAt, h.ProjectedAt);

            Assert.AreEqual(isPending, h.IsPending());
        }

        [Test]
        public void should_delete()
        {
            _writer.Create(_documentHandle);
            _writer.Promise(_documentHandle, Document_1, 10);
            
            _writer.Delete(_documentHandle, 11);
            var h = _writer.Get(_documentHandle);
            Assert.IsNull(h);
        }

        [Test]
        public void should_not_delete()
        {
            _writer.Create(_documentHandle);
            _writer.Promise(_documentHandle, Document_1, 10);
            
            _writer.Delete(_documentHandle, 9);
            var h = _writer.Get(_documentHandle);
            Assert.IsNotNull(h);
        }
    }
}

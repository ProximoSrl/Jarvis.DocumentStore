using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class HandleWriterTests
    {
        private IHandleWriter _writer;
        private DocumentHandle _handle = new DocumentHandle("handle_1");
        private DocumentId _doc1 = new DocumentId(1);
        private DocumentId _doc2 = new DocumentId(2);
        private DocumentStoreBootstrapper _documentStoreService;

        [SetUp]
        public void SetUp()
        {
            BsonClassMapHelper.Clear();

            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropTestsTenant();
            config.ServerAddress = TestConfig.ServerAddress;
            _documentStoreService = new DocumentStoreBootstrapper();
            _documentStoreService.Start(config);

            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _writer = tenant.Container.Resolve<IHandleWriter>();
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }


        [Test]
        public void with_promise_and_create_handle_should_be_linked_to_document_1()
        {
            _writer.Promise(_handle, 1);
            _writer.CreateIfMissing(_handle, 1);
            _writer.LinkDocument(_handle, _doc1, 2);

            var h = _writer.FindOneById(_handle);
            Assert.AreEqual(_doc1, h.DocumentId);
        }

        [Test]
        public void with_create_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.Promise(_handle, 1);
            _writer.LinkDocument(_handle, _doc1, 2);
        
            var h = _writer.FindOneById(_handle);
            Assert.AreEqual(_doc1, h.DocumentId);
        }

        [Test]
        public void with_link_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.LinkDocument(_handle, _doc1, 2);
            _writer.Promise(_handle, 1);
        
            var h = _writer.FindOneById(_handle);
            Assert.AreEqual(_doc1, h.DocumentId);
        }   
        
        [Test]
        public void promise_should_set_document_id_equals_to_null()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.LinkDocument(_handle, _doc1, 2);
            _writer.Promise(_handle, 1);

            _writer.Promise(_handle, 3);

            var h = _writer.FindOneById(_handle);
            Assert.IsNull(h.DocumentId);
        }

        [Test]
        public void first_promise_should_create_handle()
        {
            _writer.Promise(_handle, 1);
            var h = _writer.FindOneById(_handle);
            Assert.NotNull(h);
        }

        [Test]
        public void second_promise_should_unlink_document()
        {
            _writer.Promise(_handle, 1);
            _writer.LinkDocument(_handle, _doc1, 2);
            _writer.Promise(_handle, 3);

            var h = _writer.FindOneById(_handle);
            Assert.NotNull(h);
            Assert.IsNull(h.DocumentId);
        }
    }
}
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class DocumentWriterTests
    {
        private IDocumentWriter _writer;
        private DocumentHandle _handle = new DocumentHandle("handle_1");
        private DocumentHandle _handleAttach1 = new DocumentHandle("handle_2");
        private DocumentHandle _handleAttach2 = new DocumentHandle("handle_3");
        private DocumentHandle _handleAttach3 = new DocumentHandle("handle_4");
        private DocumentDescriptorId _doc1 = new DocumentDescriptorId(1);
        private DocumentDescriptorId _doc2 = new DocumentDescriptorId(2);
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
            _writer = tenant.Container.Resolve<IDocumentWriter>();
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
        public void verify_collection_of_attachments()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.CreateIfMissing(_handleAttach1, 1);
            _writer.AddAttachment(_handle, _handleAttach1);

            var h = _writer.FindOneById(_handle);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1 }));
            h = _writer.FindOneById(_handleAttach1);
            Assert.That(h.AttachmentPath, Is.EqualTo(_handle.ToString() + "/" + _handleAttach1));
        }

        [Test]
        public void verify_create_set_attachment_path()
        {
            _writer.CreateIfMissing(_handle, 1);
            var h = _writer.FindOneById(_handle);
            Assert.That(h.AttachmentPath, Is.EqualTo(_handle.ToString()));
        }


        [Test]
        public void verify_nested_attachments()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.AddAttachment(_handle, _handleAttach1);
            _writer.AddAttachment(_handleAttach1, _handleAttach2);

            var h = _writer.FindOneById(_handle);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1, _handleAttach2 }));
            Assert.That(h.DirectAttachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1 }));

            h = _writer.FindOneById(_handleAttach2);
            Assert.That(h.AttachmentPath, Is.EqualTo( _handle.ToString() + "/" + _handleAttach1 + "/" + _handleAttach2 ));
        }

        [Test]
        public void verify_delete_of_attachments()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.AddAttachment(_handle, _handleAttach1);
            _writer.AddAttachment(_handle, _handleAttach2);

            _writer.Delete(_handleAttach1, 3L);

            var h = _writer.FindOneById(_handle);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach2 }));
            Assert.That(h.DirectAttachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach2 }));
        }

        [Test]
        public void verify_delete_of_attachments_nested()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.CreateIfMissing(_handleAttach3, 4);
            _writer.AddAttachment(_handle, _handleAttach1);
            _writer.AddAttachment(_handle, _handleAttach2);
            _writer.AddAttachment(_handleAttach1, _handleAttach3);

            _writer.Delete(_handleAttach3, 3L);

            var h = _writer.FindOneById(_handle);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1, _handleAttach2 }));
            Assert.That(h.DirectAttachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1, _handleAttach2 }));

            h = _writer.FindOneById(_handleAttach1);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] {  }));
            Assert.That(h.DirectAttachments, Is.EquivalentTo(new DocumentHandle[] { }));
        }

        [Test]
        public void verify_delete_of_attachments_nested_cascade()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.CreateIfMissing(_handleAttach3, 4);
            _writer.AddAttachment(_handle, _handleAttach1);
            _writer.AddAttachment(_handle, _handleAttach2);
            _writer.AddAttachment(_handleAttach1, _handleAttach3);

            _writer.Delete(_handleAttach1, 3L);

            var h = _writer.FindOneById(_handle);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach2, _handleAttach3 }));
            Assert.That(h.DirectAttachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach2 }));

        }

        [Test]
        public void verify_delete_of_attachments_nested_intermediate()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.CreateIfMissing(_handleAttach3, 4);
            _writer.AddAttachment(_handle, _handleAttach1);
            _writer.AddAttachment(_handle, _handleAttach2);
            _writer.AddAttachment(_handleAttach1, _handleAttach3);

            _writer.Delete(_handleAttach2, 3L);

            var h = _writer.FindOneById(_handle);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1, _handleAttach3 }));
            Assert.That(h.DirectAttachments, Is.EquivalentTo(new DocumentHandle[] { _handleAttach1 }));

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
using System;
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
        private DocumentHandle _handle1 = new DocumentHandle("handle_1");
        private DocumentHandle _handle2 = new DocumentHandle("handle_2");
        private DocumentHandle _handleAttach1 = new DocumentHandle("Attach_1");
        private DocumentHandle _handleAttach2 = new DocumentHandle("Attach_2");
        private DocumentHandle _handleAttach3 = new DocumentHandle("Attach_3");
        private DocumentDescriptorId _doc1 = new DocumentDescriptorId(1);
        private DocumentDescriptorId _doc2 = new DocumentDescriptorId(2);
        private DocumentDescriptorId _doc3 = new DocumentDescriptorId(3);
        private DocumentDescriptorId _doc4 = new DocumentDescriptorId(4);
        private DocumentStoreBootstrapper _documentStoreService;

        [SetUp]
        public void SetUp()
        {
            BsonClassMapHelper.Clear();

            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropTestsTenant();
            config.SetTestAddress(TestConfig.ServerAddress);
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
            _writer.Promise(_handle1, 1);
            _writer.CreateIfMissing(_handle1, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);

            var h = _writer.FindOneById(_handle1);
            Assert.AreEqual(_doc1, h.DocumentDescriptorId);
        }

        [Test]
        public void with_create_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.Promise(_handle1, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);
        
            var h = _writer.FindOneById(_handle1);
            Assert.AreEqual(_doc1, h.DocumentDescriptorId);
        }

        [Test]
        public void with_link_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);
            _writer.Promise(_handle1, 1);
        
            var h = _writer.FindOneById(_handle1);
            Assert.AreEqual(_doc1, h.DocumentDescriptorId);
        }

        [Test]
        public void de_duplication_set_handle_as_de_duplicated()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handle2, 3);
            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.DocumentDeDuplicated(_handle2, _doc1, null, 6);

            var h = _writer.FindOneById(_handle2);
            Assert.That(h.DeDuplicated, Is.EqualTo(true));
        }

        [Test]
        public void verify_collection_of_attachments()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handleAttach1, 1);
            _writer.AddAttachment(_handle1, _handleAttach1);

            var h = _writer.FindOneById(_handle1);
            Assert.That(h.Attachments, Is.EquivalentTo(new [] { _handleAttach1 }));
        }

        [Test]
        public void verify_attachment_de_duplication()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handle2, 3);
            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.AddAttachment(_handle1, _handleAttach1);
            //_handle2 is de_duplicated, it should gain all attachment of the original doc.
            _writer.DocumentDeDuplicated(_handle2, _doc1, null, 6);

            var h = _writer.FindOneById(_handle2);
            Assert.That(h.Attachments, Is.EquivalentTo(new[] { _handleAttach1 }), "When an attachment is de duplicated it should gain all attachment of the previous handle");
        }

        [Test]
        public void verify_de_duplication_then_attach()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handle2, 3);
            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.DocumentDeDuplicated(_handle2, _doc1, null, 6);

            //after de duplication the worker started adding attachments to handle1
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.AddAttachment(_handle1, _handleAttach1);

            var h = _writer.FindOneById(_handle2);
            Assert.That(h.Attachments, Is.EquivalentTo(new[] { _handleAttach1 }), "When an attachment is added to handle, all duplicated handle should have same attachment");
        }

        [Test]
        public void verify_de_duplication_then__multiple_attach()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handle2, 3);
            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.DocumentDeDuplicated(_handle2, _doc1, null, 6);

            //after de duplication the worker started adding attachments to handle1
            _writer.CreateIfMissing(_handleAttach1, 7);
            _writer.AddAttachment(_handle1, _handleAttach1);

            _writer.CreateIfMissing(_handleAttach2, 8);
            _writer.AddAttachment(_handle2, _handleAttach2);

            var h = _writer.FindOneById(_handle2);
            Assert.That(h.Attachments, Is.EquivalentTo(new[] { _handleAttach1 }), "When an attachment is added to secondary handle no need to attach again.");
        }
       
        [Test]
        public void verify_delete_of_attachments()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.AddAttachment(_handle1, _handleAttach1);
            _writer.AddAttachment(_handle1, _handleAttach2);

            _writer.Delete(_handleAttach1, 3L);

            var h = _writer.FindOneById(_handle1);
            Assert.That(h.Attachments, Is.EquivalentTo(new[] { _handleAttach2 }));
        }

        [Test]
        public void verify_delete_of_attachments_nested()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.CreateIfMissing(_handleAttach3, 4);

            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.LinkDocument(_handleAttach1, _doc2, 6);
            _writer.LinkDocument(_handleAttach2, _doc3, 7);
            _writer.LinkDocument(_handleAttach3, _doc4, 8);

            _writer.AddAttachment(_handle1, _handleAttach1);
            _writer.AddAttachment(_handle1, _handleAttach2);
            _writer.AddAttachment(_handleAttach1, _handleAttach3);

            _writer.Delete(_handleAttach3, 9L);

            var h = _writer.FindOneById(_handle1);
            Assert.That(h.Attachments, Is.EquivalentTo(new[] { _handleAttach1, _handleAttach2 }));

            h = _writer.FindOneById(_handleAttach1);
            Assert.That(h.Attachments, Is.EquivalentTo(new DocumentHandle[] { }));
        }

        [Test]
        public void verify_delete_of_attachments_nested_cascade()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.CreateIfMissing(_handleAttach3, 4);

            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.LinkDocument(_handleAttach1, _doc2, 6);
            _writer.LinkDocument(_handleAttach2, _doc3, 7);
            _writer.LinkDocument(_handleAttach3, _doc4, 8);

            _writer.AddAttachment(_handle1, _handleAttach1);
            _writer.AddAttachment(_handle1, _handleAttach2);
            _writer.AddAttachment(_handleAttach1, _handleAttach3);

            _writer.Delete(_handleAttach1, 9L);

            var h = _writer.FindOneById(_handle1);
            Assert.That(h.Attachments, Is.EquivalentTo(new[] { _handleAttach2 }));

        }

        [Test]
        public void verify_delete_of_attachments_nested_intermediate()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.CreateIfMissing(_handleAttach1, 2);
            _writer.CreateIfMissing(_handleAttach2, 3);
            _writer.CreateIfMissing(_handleAttach3, 4);

            _writer.LinkDocument(_handle1, _doc1, 5);
            _writer.LinkDocument(_handleAttach1, _doc2, 6);
            _writer.LinkDocument(_handleAttach2, _doc3, 7);
            _writer.LinkDocument(_handleAttach3, _doc4, 8);

            _writer.AddAttachment(_handle1, _handleAttach1);
            _writer.AddAttachment(_handle1, _handleAttach2);
            _writer.AddAttachment(_handleAttach1, _handleAttach3);

            _writer.Delete(_handleAttach2, 9L);

            var h = _writer.FindOneById(_handle1);
            Assert.That(h.Attachments, Is.EquivalentTo(new [] { _handleAttach1 }));

        }

        [Test]
        public void promise_should_set_document_id_equals_to_null()
        {
            _writer.CreateIfMissing(_handle1, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);
            _writer.Promise(_handle1, 1);

            _writer.Promise(_handle1, 3);

            var h = _writer.FindOneById(_handle1);
            Assert.IsNull(h.DocumentDescriptorId);
        }

        [Test]
        public void first_promise_should_create_handle()
        {
            _writer.Promise(_handle1, 1);
            var h = _writer.FindOneById(_handle1);
            Assert.NotNull(h);
        }

        [Test]
        public void second_promise_should_unlink_document()
        {
            _writer.Promise(_handle1, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);
            _writer.Promise(_handle1, 3);

            var h = _writer.FindOneById(_handle1);
            Assert.NotNull(h);
            Assert.IsNull(h.DocumentDescriptorId);
        }
    }
}
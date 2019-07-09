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
using System.Linq;
using System.Collections.Generic;
using Jarvis.DocumentStore.Shared.Jobs;
using System.Threading.Tasks;

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
            config.SetTestAddress(TestConfig.TestHostServiceAddress);
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
            _writer.CreateIfMissing(_handle1, null, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);

            var h = _writer.FindOneById(_handle1);
            Assert.AreEqual(_doc1, h.DocumentDescriptorId);
        }

        [Test]
        public void with_create_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle1, null, 1);
            _writer.Promise(_handle1, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);

            var h = _writer.FindOneById(_handle1);
            Assert.AreEqual(_doc1, h.DocumentDescriptorId);
        }

        [Test]
        public void with_link_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle1, null, 1);
            _writer.LinkDocument(_handle1, _doc1, 2);
            _writer.Promise(_handle1, 1);

            var h = _writer.FindOneById(_handle1);
            Assert.AreEqual(_doc1, h.DocumentDescriptorId);
        }

        [Test]
        public void promise_should_set_document_id_equals_to_null()
        {
            _writer.CreateIfMissing(_handle1, null, 1);
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

        /// <summary>
        /// Probably this test is not the very best you can do, because
        /// it is not guarantee to fail if we have multithread concurrency,
        /// but it fails almost all the time when we have bug so it is better
        /// than nothin.
        /// </summary>
        [Test]
        public void verify_that_create_if_missing_is_thread_safe()
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var sequence = Enumerable.Range(0, 10);
                    Parallel.ForEach(sequence, j =>
                    {
                        var handle = new DocumentHandle("handle: " + i.ToString());
                        _writer.CreateIfMissing(handle, null, 1);
                    });
                }
                catch (Exception ex)
                {
                    Assert.Fail("Exception at iteration " + i + ": " + ex.ToString());
                }
            }
        }
    }
}
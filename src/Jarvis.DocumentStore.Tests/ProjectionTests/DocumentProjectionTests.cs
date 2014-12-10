using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;
using DocumentHandle = Jarvis.DocumentStore.Core.Model.DocumentHandle;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class DocumentProjectionTests
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private ICommandBus _bus;
        IBlobStore _filestore;
        IHandleWriter _handleWriter;
        IReader<DocumentReadModel, DocumentId> _documentReader;
        private ITriggerProjectionsUpdate _projections;
        [SetUp]
        public void SetUp()
        {
            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropTestsTenant();
            
            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _documentStoreService.Start(config);

            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _bus = tenant.Container.Resolve<ICommandBus>();
            _filestore = tenant.Container.Resolve<IBlobStore>();
            Assert.IsTrue(_bus is IInProcessCommandBus);
            _projections = tenant.Container.Resolve<ITriggerProjectionsUpdate>();
            _handleWriter = tenant.Container.Resolve<IHandleWriter>();
            _documentReader = tenant.Container.Resolve<IReader<DocumentReadModel, DocumentId>>();
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        void CreateDocument(int id,string handle, string pathToFile)
        {
            var fname = Path.GetFileName(pathToFile);
            var info = new DocumentHandleInfo(new DocumentHandle(handle), new FileNameWithExtension(fname));
            _bus.Send(new CreateDocument(
                new DocumentId(id),
                _filestore.Upload(Core.Processing.DocumentFormats.Original, pathToFile),
                info,
                new FileHash("1234abcd"),
                new FileNameWithExtension("a","file")
            ));
            Thread.Sleep(50);
        }

        [Test]
        public async void should_deduplicate()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPng);

            await _projections.UpdateAndWait();

            var list = _handleWriter.AllSortedByHandle.ToArray();
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(new DocumentHandle("handle"), list[0].Handle);
            Assert.AreEqual(new DocumentHandle("handle_bis"), list[1].Handle);

            Assert.AreEqual(new DocumentId(1), list[0].DocumentId);
            Assert.AreEqual(new DocumentId(1), list[1].DocumentId);
        }

        [Test]
        public async void should_remove_handle_previous_document()
        {
            CreateDocument(1, "handle_bis", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var old_handle_bis_document = _documentReader.FindOneById(new DocumentId(1));
            Assert.IsNull(old_handle_bis_document);

            var new_handle_bis_document = _documentReader.FindOneById(new DocumentId(2));
            Assert.NotNull(new_handle_bis_document);
        }

        [Test]
        public async void should_remove_orphaned_document()
        {
            CreateDocument(1, "handle_bis", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPdf);
            CreateDocument(3, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var old_handle_bis_document = _documentReader.FindOneById(new DocumentId(1));
            Assert.IsNull(old_handle_bis_document);

            var new_handle_bis_document = _documentReader.FindOneById(new DocumentId(2));
            Assert.NotNull(new_handle_bis_document);
        }

        [Test]
        public async void should_deduplicate_twice()
        {
            CreateDocument(9, "handle", TestConfig.PathToDocumentPdf);
            CreateDocument(10, "handle", TestConfig.PathToDocumentPdf);
            CreateDocument(11, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var original = _documentReader.FindOneById(new DocumentId(9));
            var copy = _documentReader.FindOneById(new DocumentId(10));
            var copy2 = _documentReader.FindOneById(new DocumentId(11));

            var handle = _handleWriter.FindOneById(new DocumentHandle("handle"));

            Assert.IsNotNull(original);
            Assert.IsNull(copy);
            Assert.IsNull(copy2);
            
            Assert.IsNotNull(handle);
            Assert.AreEqual(handle.DocumentId, new DocumentId(9));
        }       
        
        [Test]
        public async void should_deduplicate_a_document_with_same_content_and_handle()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPdf);
            CreateDocument(2, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var original = _documentReader.FindOneById(new DocumentId(1));
            Assert.IsNotNull(original);

            var handle = _handleWriter.FindOneById(new DocumentHandle("handle"));
            Assert.IsNotNull(handle);
            Assert.AreEqual(handle.DocumentId, new DocumentId(1));

            var copy = _documentReader.FindOneById(new DocumentId(2));
            Assert.IsNull(copy);
        }     
        
        [Test]
        public async void should_create_a_document()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var original = _documentReader.FindOneById(new DocumentId(1));
            Assert.IsNotNull(original);

            var handle = _handleWriter.FindOneById(new DocumentHandle("handle"));
            Assert.IsNotNull(handle);
            Assert.AreEqual(handle.DocumentId, new DocumentId(1));
        }
    }
}

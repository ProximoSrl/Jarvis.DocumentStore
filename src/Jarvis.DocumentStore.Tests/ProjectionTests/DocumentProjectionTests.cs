using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class DocumentProjectionTests
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private ICommandBus _bus;
        IFileStore _filestore;
        IReader<HashToDocuments, FileHash> _hashReader;
        IReader<HandleToDocument, DocumentHandle> _handleReader;
        IReader<DocumentReadModel, DocumentId> _documentReader;


        [SetUp]
        public void SetUp()
        {
            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropTenant1();
            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _documentStoreService.Start(config);

            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _bus = tenant.Container.Resolve<ICommandBus>();
            _filestore = tenant.Container.Resolve<IFileStore>();
            Assert.IsTrue(_bus is IInProcessCommandBus);

            _hashReader = tenant.Container.Resolve<IReader<HashToDocuments, FileHash>>();
            _handleReader = tenant.Container.Resolve<IReader<HandleToDocument, DocumentHandle>>();
            _documentReader = tenant.Container.Resolve<IReader<DocumentReadModel, DocumentId>>();
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        FileId Upload(string id, string pathToFile)
        {
            var fileId = new FileId(id);
            _filestore.Upload(fileId, pathToFile );
            return fileId;
        }

        void CreateDocument(int id,string handle, string pathToFile)
        {
            var fname = Path.GetFileName(pathToFile);
            _bus.Send(new CreateDocument(
                new DocumentId(id),
                Upload(handle, pathToFile),
                new DocumentHandle(handle),
                new FileNameWithExtension(fname), 
                null
            ));
        }

        [Test]
        public void should_deduplicate()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPng);

            //CreateDocument(1, "handle_1", TestConfig.PathToDocumentPdf);
            //CreateDocument(3, "handle_3", TestConfig.PathToOpenDocumentSpreadsheet);

            Thread.Sleep(1000);

            Assert.AreEqual(1, _hashReader.AllSortedById.Count());

            var list = _handleReader.AllSortedById.ToArray();
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(new DocumentHandle("handle"), list[0].Id);
            Assert.AreEqual(new DocumentHandle("handle_bis"), list[1].Id);

            Assert.AreEqual(new DocumentId(1), list[0].DocumentId);
            Assert.AreEqual(new DocumentId(1), list[1].DocumentId);
        }

        [Test]
        public void should_remove_old_document()
        {
            CreateDocument(1, "handle_bis", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPdf);
            Thread.Sleep(1000);

            var old_handle_bis_document = _documentReader.FindOneById(new DocumentId(1));
            Assert.IsNull(old_handle_bis_document);

            var new_handle_bis_document = _documentReader.FindOneById(new DocumentId(2));
            Assert.NotNull(new_handle_bis_document);
            Assert.AreEqual(1, new_handle_bis_document.HandlesCount);
        }

        [Test]
        public void should_remove_old()
        {
            CreateDocument(1, "handle_bis", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPdf);
            CreateDocument(3, "handle", TestConfig.PathToDocumentPdf);
            Thread.Sleep(5000);

            var old_handle_bis_document = _documentReader.FindOneById(new DocumentId(1));
            Assert.IsNull(old_handle_bis_document);

            var new_handle_bis_document = _documentReader.FindOneById(new DocumentId(2));
            Assert.NotNull(new_handle_bis_document);
            Assert.AreEqual(1, new_handle_bis_document.HandlesCount);
        }
    }
}

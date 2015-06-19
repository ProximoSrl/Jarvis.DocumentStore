using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.Commands;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using NUnit.Framework;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;
using DocumentHandle = Jarvis.DocumentStore.Core.Model.DocumentHandle;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class DocumentDescriptorProjectionTests
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private ICommandBus _bus;
        IBlobStore _filestore;
        IDocumentWriter _handleWriter;
        IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentReader;
        private ITriggerProjectionsUpdate _projections;

        [SetUp]
        public void SetUp()
        {
            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropTestsTenant();
            config.SetTestAddress(TestConfig.ServerAddress);
            _documentStoreService = new DocumentStoreBootstrapper();
            _documentStoreService.Start(config);

            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _bus = tenant.Container.Resolve<ICommandBus>();
            _filestore = tenant.Container.Resolve<IBlobStore>();
            Assert.IsTrue(_bus is IInProcessCommandBus);
            _projections = tenant.Container.Resolve<ITriggerProjectionsUpdate>();
            _handleWriter = tenant.Container.Resolve<IDocumentWriter>();
            _documentReader = tenant.Container.Resolve<IReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        void CreateDocument(int id, string handle, string pathToFile)
        {
            var fname = Path.GetFileName(pathToFile);
            var info = new DocumentHandleInfo(new DocumentHandle(handle), new FileNameWithExtension(fname));
            _bus.Send(new InitializeDocumentDescriptor(
                new DocumentDescriptorId(id),
                _filestore.Upload(DocumentFormats.Original, pathToFile),
                info,
                new FileHash("1234abcd"),
                new FileNameWithExtension("a","file")
            ));
            Thread.Sleep(50);
        }

        BlobId AddFormatToDocument(int id, string handle, DocumentFormat format, PipelineId pipelineId, string pathToFile)
        {
            var fname = Path.GetFileName(pathToFile);
            var info = new DocumentHandleInfo(new DocumentHandle(handle), new FileNameWithExtension(fname));
            var blobId = _filestore.Upload(format, pathToFile);
            _bus.Send(new AddFormatToDocumentDescriptor(
                new DocumentDescriptorId(id),
                format,
                blobId,
                pipelineId
            ));
            Thread.Sleep(50);
            return blobId;
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

            Assert.AreEqual(new DocumentDescriptorId(1), list[0].DocumentDescriptorId);
            Assert.AreEqual(new DocumentDescriptorId(1), list[1].DocumentDescriptorId);
        }

        [Test]
        public async void should_remove_handle_previous_document()
        {
            CreateDocument(1, "handle_bis", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var old_handle_bis_document = _documentReader.FindOneById(new DocumentDescriptorId(1));
            Assert.IsNull(old_handle_bis_document);

            var new_handle_bis_document = _documentReader.FindOneById(new DocumentDescriptorId(2));
            Assert.NotNull(new_handle_bis_document);
        }

        [Test]
        public async void should_remove_orphaned_document()
        {
            CreateDocument(1, "handle_bis", TestConfig.PathToDocumentPng);
            CreateDocument(2, "handle_bis", TestConfig.PathToDocumentPdf);
            CreateDocument(3, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var old_handle_bis_document = _documentReader.FindOneById(new DocumentDescriptorId(1));
            Assert.IsNull(old_handle_bis_document);

            var new_handle_bis_document = _documentReader.FindOneById(new DocumentDescriptorId(2));
            Assert.NotNull(new_handle_bis_document);
        }

        [Test]
        public async void should_deduplicate_twice()
        {
            CreateDocument(9, "handle", TestConfig.PathToDocumentPdf);
            CreateDocument(10, "handle", TestConfig.PathToDocumentPdf);
            CreateDocument(11, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var original = _documentReader.FindOneById(new DocumentDescriptorId(9));
            var copy = _documentReader.FindOneById(new DocumentDescriptorId(10));
            var copy2 = _documentReader.FindOneById(new DocumentDescriptorId(11));

            var handle = _handleWriter.FindOneById(new DocumentHandle("handle"));

            Assert.IsNotNull(original);
            Assert.IsNull(copy);
            Assert.IsNull(copy2);
            
            Assert.IsNotNull(handle);
            Assert.AreEqual(handle.DocumentDescriptorId, new DocumentDescriptorId(9));
        }       
        
        [Test]
        public async void should_deduplicate_a_document_with_same_content_and_handle()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPdf);
            CreateDocument(2, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var original = _documentReader.FindOneById(new DocumentDescriptorId(1));
            Assert.IsNotNull(original);

            var handle = _handleWriter.FindOneById(new DocumentHandle("handle"));
            Assert.IsNotNull(handle);
            Assert.AreEqual(handle.DocumentDescriptorId, new DocumentDescriptorId(1));

            var copy = _documentReader.FindOneById(new DocumentDescriptorId(2));
            Assert.IsNull(copy);
        }     
        
        [Test]
        public async void should_create_a_documentDescriptor()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPdf);
            await _projections.UpdateAndWait();

            var original = _documentReader.FindOneById(new DocumentDescriptorId(1));
            Assert.IsNotNull(original);

            var handle = _handleWriter.FindOneById(new DocumentHandle("handle"));
            Assert.IsNotNull(handle);
            Assert.AreEqual(handle.DocumentDescriptorId, new DocumentDescriptorId(1));
        }

        [Test]
        public async void should_add_format_to_document()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPng);
            var format = new DocumentFormat("tika");
            var blobId = AddFormatToDocument(1, "handle", format, new PipelineId("tika"), TestConfig.PathToTextDocument);

            await _projections.UpdateAndWait();

            var document = _documentReader.AllUnsorted.Single(d => d.Id == new DocumentDescriptorId(1));
            Assert.That(document.Formats, Has.Count.EqualTo(2));
            Assert.That(document.Formats[format], Is.Not.Null, "Document has not added format");
            Assert.That(document.Formats[format].BlobId, Is.EqualTo(blobId), "Wrong BlobId");
        }

        [Test]
        public async void adding_twice_same_format_overwrite_format()
        {
            CreateDocument(1, "handle", TestConfig.PathToDocumentPng);
            var format = new DocumentFormat("tika");
            var blobId1 = AddFormatToDocument(1, "handle", format, new PipelineId("tika"), TestConfig.PathToTextDocument);
            var blobId2 = AddFormatToDocument(1, "handle", format, new PipelineId("tika"), TestConfig.PathToHtml);

            await _projections.UpdateAndWait();

            var document = _documentReader.AllUnsorted.Single(d => d.Id == new DocumentDescriptorId(1));
            Assert.That(document.Formats, Has.Count.EqualTo(2));
            Assert.That(document.Formats[format], Is.Not.Null, "Document has not added format");
            Assert.That(document.Formats[format].BlobId, Is.EqualTo(blobId2), "Wrong BlobId");
        }
    }
}

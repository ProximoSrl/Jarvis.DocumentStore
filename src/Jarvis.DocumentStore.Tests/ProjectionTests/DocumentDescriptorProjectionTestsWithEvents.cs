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
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using System;
using Jarvis.DocumentStore.Core.EventHandlers;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using Jarvis.Framework.Shared.Domain.Serialization;
using Jarvis.DocumentStore.Tests.Support;

using Jarvis.Framework.Shared.Helpers;


using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class DocumentDescriptorProjectionTestsWithEvents
    {
        IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentReader;
        private ITriggerProjectionsUpdate _projections;

        DocumentDescriptorProjection _sut;

        [SetUp]
        public void SetUp()
        {
            MongoDbTestConnectionProvider.ReadModelDb.Drop();

            var mngr = new IdentityManager(new CounterService(MongoDbTestConnectionProvider.ReadModelDb));
            mngr.RegisterIdentitiesFromAssembly(typeof(DocumentDescriptorId).Assembly);

            MongoFlatIdSerializerHelper.Initialize(mngr);

            EventStoreIdentityCustomBsonTypeMapper.Register<DocumentDescriptorId>();
            EventStoreIdentityCustomBsonTypeMapper.Register<DocumentId>();
            StringValueCustomBsonTypeMapper.Register<BlobId>();
            StringValueCustomBsonTypeMapper.Register<DocumentHandle>();
            StringValueCustomBsonTypeMapper.Register<FileHash>();

            var _writer = new DocumentWriter(MongoDbTestConnectionProvider.ReadModelDb);
            var _documentDescriptorCollection = new CollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId>
                (
                    new MongoStorageFactory(MongoDbTestConnectionProvider.ReadModelDb,
                        new RebuildContext(false)), null);
            _documentReader = new MongoReader<DocumentDescriptorReadModel, DocumentDescriptorId>(MongoDbTestConnectionProvider.ReadModelDb);
            _sut = new DocumentDescriptorProjection(_documentDescriptorCollection, _writer);
        }

        [TearDown]
        public void TearDown()
        {
            BsonClassMapHelper.Clear();
        }

        [Test]
        public void add_attachment_to_document_descriptor()
        {
            ElaborateDocumentDescriptorInitialized(1);
            ElaborateDocumentDescriptorInitialized(2);
            var attachmentHandle = new DocumentHandle("Attachment");
            _sut.On(new DocumentDescriptorHasNewAttachment(attachmentHandle, "Path.txt")
                .AssignIdForTest(new DocumentDescriptorId(1)));

            var documentDescriptor = _documentReader.AllUnsorted.Single(d => d.Id == new DocumentDescriptorId(1));
            Assert.That(documentDescriptor.Attachments.Select(a => a.Handle),
                Is.EquivalentTo(new[] { attachmentHandle }));
        }

    

        //[Test]
        //public void add_attachment_then_delete()
        //{
        //    _sut.On(new DocumentInitialized(_documentHandle).AssignIdForTest(_documentId1));
        //    _sut.On(new DocumentInitialized(_attachmentHandle).AssignIdForTest(_document2));
        //    _sut.On(new DocumentHasNewAttachment(_documentHandle, _attachmentHandle).AssignIdForTest(_documentId2));
        //    _sut.On(new DocumentDeleted(_attachmentHandle, _document2).AssignIdForTest(_documentId1));

        //    var h = _writer.FindOneById(_documentHandle);
        //    Assert.That(h.Attachments, Is.Empty);
        //}

        private void ElaborateDocumentDescriptorInitialized(Int64 id)
        {
            _sut.On(new DocumentDescriptorInitialized(
                new BlobId("original." + id.ToString()),
                new DocumentHandleInfo(
                    new DocumentHandle("file" + id.ToString()),
                    new FileNameWithExtension("file" + id.ToString() + ".txt")), 
                new FileHash("xxxxxx"))
                .AssignIdForTest(new DocumentDescriptorId(id)));
        }
    }
}

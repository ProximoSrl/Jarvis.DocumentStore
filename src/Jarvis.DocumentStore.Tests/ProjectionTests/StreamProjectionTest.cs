using System;
using System.Collections.Generic;
using System.Linq;
using Fasterflect;
using javax.swing.@event;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.EventHandlers;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.Events;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using NSubstitute;
using NUnit.Framework;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.Framework.TestHelpers;
using org.objectweb.asm;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class StreamProjectionTest
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private StreamProjection _sut;
        private ICollectionWrapper<StreamReadModel, Int64> _collectionWrapper;
        private IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _readerDocumentReadModel;
        private IDocumentWriter _handleWriter;
        private IBlobStore _blobStore;
        private List<StreamReadModel> rmStream;
        private List<DocumentDescriptorReadModel> rmDocuments;
        private DocumentReadModel handle;

        [SetUp]
        public void SetUp()
        {
            _collectionWrapper = Substitute.For<ICollectionWrapper<StreamReadModel, Int64>>();
            rmStream = new List<StreamReadModel>();
            rmDocuments = new List<DocumentDescriptorReadModel>();

            _collectionWrapper.When(r => r.Insert(
                Arg.Any<DomainEvent>(),
                Arg.Any<StreamReadModel>()))
                .Do(cinfo => rmStream.Add((StreamReadModel)cinfo.Args()[1]));
            _collectionWrapper.All.Returns(rmStream.AsQueryable());

            _readerDocumentReadModel = Substitute.For<IReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();
            _readerDocumentReadModel.AllUnsorted.Returns(rmDocuments.AsQueryable());
            _readerDocumentReadModel.AllSortedById.Returns(rmDocuments.AsQueryable().OrderBy(r => r.Id));
            _readerDocumentReadModel.FindOneById(Arg.Any<DocumentDescriptorId>())
                .Returns(cinfo => rmDocuments.SingleOrDefault(d => d.Id == (DocumentDescriptorId)cinfo.Args()[0]));

            _handleWriter = Substitute.For<IDocumentWriter>();
            _blobStore = Substitute.For<IBlobStore>();
        }

        private void CreateSut()
        {
            _sut = new StreamProjection(_collectionWrapper, _handleWriter, _blobStore, _readerDocumentReadModel);
            _sut.TenantId = new TenantId("test-tenant");
        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void verify_id_when_empty_projection()
        {
            CreateSut();
            var evt = new DocumentDescriptorCreated(new BlobId("original.1"),  new DocumentHandle("Rev_1"))
                .AssignIdForTest(new DocumentId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].Id, Is.EqualTo(1));
        }



        [Test]
        public void verify_pipeline_id_is_original_when_pipeline_is_null()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(
                new DocumentHandle("rev_1"),
                new DocumentDescriptorId(1),
                new DocumentDescriptorId(2),
                new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].FormatInfo.PipelineId, Is.EqualTo(new PipelineId("original")));
        }

        [Test]
        public void verify_document_descriptor_initialized_not_generates_record()
        {
            CreateSut();
            var evt = new DocumentInitialized( new DocumentHandle("rev_1")).AssignIdForTest(new DocumentId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(0), "Document Initialized is raised when document descriptor still is not de-duplicated.");
        }

        [Test]
        public void verify_document_descriptor_created_generates_record()
        {
            CreateSut();
            var evt = new DocumentDescriptorCreated(new BlobId("original.1"), new DocumentHandle("rev_1")).AssignIdForTest(new DocumentDescriptorId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentCreated));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
        }

        [Test]
        public void verify_document_descriptor_de_duplicated_generates_record()
        {
            CreateSut();
            var evt = new DocumentDescriptorHasBeenDeduplicated(new DocumentDescriptorId(2),  new DocumentHandle("rev_2"), new FileNameWithExtension("test.pdf"))
                .AssignIdForTest(new DocumentDescriptorId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentCreated));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_2"));
        }

        [Test]
        public void verify_stream_events_have_fileName()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(new DocumentHandle("rev_1"), new DocumentDescriptorId(1), new DocumentDescriptorId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));
        }

        [Test]
        public void verify_stream_events_have_custom_handle_data()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(new DocumentHandle("rev_1"), new DocumentDescriptorId(1), new DocumentDescriptorId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].DocumentCustomData, Is.EqualTo(handle.CustomData));
    
        }

        [Test]
        public void verify_stream_events_have_documentId()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("blob_test"), new DocumentHandle("rev_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(new DocumentHandle("rev_1"), new DocumentDescriptorId(1), new DocumentDescriptorId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));

            Assert.That(rmStream[0].DocumentId, Is.EqualTo(new DocumentDescriptorId(1)));
        }

        [Test]
        public void verify_stream_events_on_attachment()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            var rev1 = new DocumentHandle("rev_1");
            docRm.AddHandle(rev1);
            rmDocuments.Add(docRm);

            var docRmAttach = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_2"), new DocumentHandle("rev_2"));
            docRmAttach.AddHandle(new DocumentHandle("rev_2"));
            rmDocuments.Add(docRmAttach);
            CreateSut();
            var evt = new DocumentHasNewAttachment(rev1, new DocumentHandle("rev_2")).AssignIdForTest(new DocumentId(1));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewAttachment));
            Assert.That(rmStream[0].EventData[StreamReadModelEventDataKeys.FatherHandle], Is.EqualTo(rev1));
        }

        private void SetHandleToReturn()
        {
            var customData = new DocumentCustomData() 
            {
                {"handle1" , "test"},
                {"handle2" , new { isComplex = true, theTruth = 42} },
            };
            handle = new DocumentReadModel(
                    new DocumentHandle("rev_1"),
                    new DocumentDescriptorId(1),
                    new FileNameWithExtension("test.txt"),
                    customData
                );
            _handleWriter
                .FindOneById(Arg.Any<DocumentHandle>())
                .Returns(handle);
            IBlobDescriptor stub = Substitute.For<IBlobDescriptor>();
            stub.FileNameWithExtension.Returns(new FileNameWithExtension("test.txt"));
            _blobStore.GetDescriptor(Arg.Any<BlobId>()).Returns(stub);
        }

        [Test]
        public void verify_id_is_sequential()
        {
            rmStream.Add(new StreamReadModel() { Id = 41 });
            CreateSut();
            var evt = new DocumentDescriptorCreated(new BlobId("blob.1"), new  DocumentHandle("rev_1")).AssignIdForTest(new DocumentId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(2));
            Assert.That(rmStream[1].Id, Is.EqualTo(42));
        }

        [Test]
        public void verify_handle_deleted()
        {
            CreateSut();
            var evt = new DocumentDeleted(new DocumentHandle("rev_1"), new DocumentDescriptorId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentDeleted));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
        }

        [Test]
        public void verify_handle_linked_to_document_with_formats()
        {
            SetHandleToReturn();
            IBlobDescriptor stub = Substitute.For<IBlobDescriptor>();
            stub.FileNameWithExtension.Returns(new FileNameWithExtension("test.txt"));
            _blobStore.GetDescriptor(Arg.Any<BlobId>()).Returns(stub);

            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            docRm.AddFormat(new PipelineId("tika"), new DocumentFormat("blah"), new BlobId("pdf"));
            docRm.AddFormat(new PipelineId("test"), new DocumentFormat("blah blah"), new BlobId("test"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(new DocumentHandle("rev_1"), new DocumentDescriptorId(1), new DocumentDescriptorId(2), new FileNameWithExtension("test.txt"));

            _sut.Handle(evt, false); //I'm expecting new format added to handle
            Assert.That(rmStream, Has.Count.EqualTo(3));

            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[0].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("original"));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));

            Assert.That(rmStream[1].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[1].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[1].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah"));
            Assert.That(rmStream[1].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[1].Filename.Extension, Is.EqualTo("txt"));

            Assert.That(rmStream[2].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[2].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[2].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah blah"));
            Assert.That(rmStream[2].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[2].Filename.Extension, Is.EqualTo("txt"));
        }

        [Test]
        public void verify_format_added_to_handle_when_added_to_document()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(new DocumentHandle("rev_1"), new DocumentDescriptorId(1), new DocumentDescriptorId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.

            var evtFormat = new FormatAddedToDocumentDescriptor(new DocumentFormat("blah"), new BlobId("test"),
                new PipelineId("tika"));
            evtFormat.AssignIdForTest(new DocumentDescriptorId(1));
            _sut.Handle(evtFormat, false); //format is linked to document.

            Assert.That(rmStream, Has.Count.EqualTo(2));

            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[0].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("original"));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));
            Assert.That(rmStream[0].FormatInfo.BlobId, Is.EqualTo(new BlobId("file_1")));
            Assert.That(rmStream[0].DocumentId, Is.EqualTo(new DocumentDescriptorId(1)));

            Assert.That(rmStream[1].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[1].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[1].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah"));
            Assert.That(rmStream[1].FormatInfo.BlobId.ToString(), Is.EqualTo("test"));
            Assert.That(rmStream[1].FormatInfo.PipelineId.ToString(), Is.EqualTo("tika"));
            Assert.That(rmStream[1].Filename.FileName, Is.EqualTo("test")); //expectation returns always the same handle
            Assert.That(rmStream[1].Filename.Extension, Is.EqualTo("txt"));
            Assert.That(rmStream[1].FormatInfo.BlobId, Is.EqualTo(new BlobId("test")));
            Assert.That(rmStream[1].DocumentId, Is.EqualTo(new DocumentDescriptorId(1)));
        }

        [Test]
        public void verify_format_updated()
        {
            SetHandleToReturn();
            var docRm = new DocumentDescriptorReadModel(new DocumentDescriptorId(1), new BlobId("file_1"), new DocumentHandle("rev_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new DocumentLinked(new DocumentHandle("rev_1"), new DocumentDescriptorId(1), new DocumentDescriptorId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.

            var evtFormat = new FormatAddedToDocumentDescriptor(new DocumentFormat("blah"), new BlobId("test.1"), new PipelineId("tika"));
            evtFormat.AssignIdForTest(new DocumentDescriptorId(1));
             _sut.Handle(evtFormat, false); //format is linked to document.

             var evtFormatUpdated = new DocumentFormatHasBeenUpdated(new DocumentFormat("blah"), new BlobId("test.2"), new PipelineId("tika"));
             evtFormatUpdated.AssignIdForTest(new DocumentDescriptorId(1));
             _sut.Handle(evtFormatUpdated, false); //format is linked to document.

            Assert.That(rmStream, Has.Count.EqualTo(3));

            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[0].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("original"));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));
            Assert.That(rmStream[0].FormatInfo.BlobId, Is.EqualTo(new BlobId("file_1")));
            Assert.That(rmStream[0].DocumentId, Is.EqualTo(new DocumentDescriptorId(1)));

            Assert.That(rmStream[1].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentHasNewFormat));
            Assert.That(rmStream[1].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[1].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah"));
            Assert.That(rmStream[1].FormatInfo.BlobId.ToString(), Is.EqualTo("test.1"));
            Assert.That(rmStream[1].FormatInfo.PipelineId.ToString(), Is.EqualTo("tika"));
            Assert.That(rmStream[1].Filename.FileName, Is.EqualTo("test")); //expectation returns always the same handle
            Assert.That(rmStream[1].Filename.Extension, Is.EqualTo("txt"));
            Assert.That(rmStream[1].DocumentId, Is.EqualTo(new DocumentDescriptorId(1)));

            Assert.That(rmStream[2].EventType, Is.EqualTo(HandleStreamEventTypes.DocumentFormatUpdated));
            Assert.That(rmStream[2].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[2].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah"));
            Assert.That(rmStream[2].FormatInfo.BlobId.ToString(), Is.EqualTo("test.2"));
            Assert.That(rmStream[2].FormatInfo.PipelineId.ToString(), Is.EqualTo("tika"));
            Assert.That(rmStream[2].Filename.FileName, Is.EqualTo("test")); //expectation returns always the same handle
            Assert.That(rmStream[2].Filename.Extension, Is.EqualTo("txt"));
            Assert.That(rmStream[2].DocumentId, Is.EqualTo(new DocumentDescriptorId(1)));
        }
    }
}
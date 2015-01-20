using System;
using System.Collections.Generic;
using System.Linq;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Events;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.EventHandlers;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class StreamReadModelTest
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private StreamProjection _sut;
        private ICollectionWrapper<StreamReadModel, Int64> _collectionWrapper;
        private IReader<DocumentReadModel, DocumentId> _readerDocumentReadModel;
        private IHandleWriter _handleWriter;

        private List<StreamReadModel> rmStream;
        private List<DocumentReadModel> rmDocuments;

        [SetUp]
        public void SetUp()
        {
            _collectionWrapper = Substitute.For<ICollectionWrapper<StreamReadModel, Int64>>();
            rmStream = new List<StreamReadModel>();
            rmDocuments = new List<DocumentReadModel>();
            
            _collectionWrapper.When(r => r.Insert(
                Arg.Any<DomainEvent>(),
                Arg.Any<StreamReadModel>()))
                .Do(cinfo => rmStream.Add((StreamReadModel) cinfo.Args()[1]));
            _collectionWrapper.All.Returns(rmStream.AsQueryable());

            _readerDocumentReadModel = Substitute.For<IReader<DocumentReadModel, DocumentId>>();
            _readerDocumentReadModel.AllUnsorted.Returns(rmDocuments.AsQueryable());
            _readerDocumentReadModel.AllSortedById.Returns(rmDocuments.AsQueryable().OrderBy(r => r.Id));
            _readerDocumentReadModel.FindOneById(Arg.Any<DocumentId>())
                .Returns(cinfo => rmDocuments.SingleOrDefault(d => d.Id == (DocumentId) cinfo.Args()[0]));

            _handleWriter = Substitute.For<IHandleWriter>();
        }

        private void CreateSut()
        {
            _sut = new StreamProjection(_collectionWrapper, _handleWriter, _readerDocumentReadModel);
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
            var evt = new HandleInitialized(new HandleId(1), new DocumentHandle("Rev_1"));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void verify_pipeline_id_is_original_when_pipeline_is_null()
        {
            SetHandleToReturn();
            var docRm = new DocumentReadModel(new DocumentId(1), new BlobId("file_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new HandleLinked(
                new DocumentHandle("rev_1"), 
                new DocumentId(1), 
                new DocumentId(2), 
                new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].FormatInfo.PipelineId, Is.EqualTo(new PipelineId("original")));

        }



        [Test]
        public void verify_handle_initialized()
        {
            CreateSut();
            var evt = new HandleInitialized(new HandleId(1), new DocumentHandle("rev_1"));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.HandleInitialized));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
        }

        //[Test]
        //public void verify_stream_events_have_tenant()
        //{
        //    CreateSut();
        //    var evt = new HandleInitialized(new HandleId(1), new DocumentHandle("rev_1"));
        //    _sut.Handle(evt, false);
        //    Assert.That(rmStream, Has.Count.EqualTo(1));
        //    Assert.That(rmStream[0].TenantId.ToString(), Is.EqualTo("test-tenant"));
        //}

        [Test]
        public void verify_stream_events_have_fileName()
        {
            SetHandleToReturn();
            var docRm = new DocumentReadModel(new DocumentId(1), new BlobId("file_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new HandleLinked(new DocumentHandle("rev_1"), new DocumentId(1), new DocumentId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));
        }

        private void SetHandleToReturn()
        {
            _handleWriter.FindOneById(Arg.Any<DocumentHandle>())
                .Returns(new HandleReadModel(
                    new DocumentHandle("rev_1"),
                    new DocumentId(1),
                    new FileNameWithExtension("test.txt")
                ));
        }

        [Test]
        public void verify_id_is_sequential()
        {
            rmStream.Add(new StreamReadModel() {Id = 41});
            CreateSut();
            var evt = new HandleInitialized(new HandleId(1), new DocumentHandle("rev_1"));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(2));
            Assert.That(rmStream[1].Id, Is.EqualTo(42));
        }

        [Test]
        public void verify_handle_deleted()
        {
            CreateSut();
            var evt = new HandleDeleted(new DocumentHandle("rev_1"), new DocumentId(1));
            _sut.Handle(evt, false);
            Assert.That(rmStream, Has.Count.EqualTo(1));
            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.HandleDeleted));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
        }

        [Test]
        public void verify_handle_linked_to_document_with_formats()
        {
            _handleWriter.FindOneById(Arg.Any<DocumentHandle>())
               .Returns(cinfo => new HandleReadModel(
                   ( DocumentHandle) cinfo.Args()[0],
                   new DocumentId(1),
                   new FileNameWithExtension(cinfo.Args()[0].ToString() + ".txt")
               ));
            var docRm = new DocumentReadModel(new DocumentId(1), new BlobId("file_1"));
            docRm.AddFormat(new PipelineId("tika"), new DocumentFormat("blah"), new BlobId("pdf") );
            docRm.AddFormat(new PipelineId("test"), new DocumentFormat("blah blah"), new BlobId("test"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new HandleLinked(new DocumentHandle("rev_1"), new DocumentId(1), new DocumentId(2), new FileNameWithExtension("test.txt") );
            
            _sut.Handle(evt, false); //I'm expecting new format added to handle
            Assert.That(rmStream, Has.Count.EqualTo(3));

            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.HandleHasNewFormat));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[0].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("original"));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("rev_1"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));

            Assert.That(rmStream[1].EventType, Is.EqualTo(HandleStreamEventTypes.HandleHasNewFormat));
            Assert.That(rmStream[1].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[1].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah"));
            Assert.That(rmStream[1].Filename.FileName, Is.EqualTo("rev_1"));
            Assert.That(rmStream[1].Filename.Extension, Is.EqualTo("txt"));

            Assert.That(rmStream[2].EventType, Is.EqualTo(HandleStreamEventTypes.HandleHasNewFormat));
            Assert.That(rmStream[2].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[2].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah blah"));
            Assert.That(rmStream[2].Filename.FileName, Is.EqualTo("rev_1"));
            Assert.That(rmStream[2].Filename.Extension, Is.EqualTo("txt"));
        }

        [Test]
        public void verify_format_added_to_handle_when_added_to_document()
        {
            SetHandleToReturn();
            var docRm = new DocumentReadModel(new DocumentId(1), new BlobId("file_1"));
            docRm.AddHandle(new DocumentHandle("rev_1"));
            rmDocuments.Add(docRm);
            CreateSut();
            var evt = new HandleLinked(new DocumentHandle("rev_1"), new DocumentId(1), new DocumentId(2), new FileNameWithExtension("test.txt"));
            _sut.Handle(evt, false); //Handle is linked to document.

            var evtFormat = new FormatAddedToDocument(new DocumentFormat("blah"), new BlobId("test"),
                new PipelineId("tika"));
            evtFormat.AggregateId = new DocumentId(1);
            _sut.Handle(evtFormat, false); //format is linked to document.

            Assert.That(rmStream, Has.Count.EqualTo(2));

            Assert.That(rmStream[0].EventType, Is.EqualTo(HandleStreamEventTypes.HandleHasNewFormat));
            Assert.That(rmStream[0].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[0].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("original"));
            Assert.That(rmStream[0].Filename.FileName, Is.EqualTo("test"));
            Assert.That(rmStream[0].Filename.Extension, Is.EqualTo("txt"));

            Assert.That(rmStream[1].EventType, Is.EqualTo(HandleStreamEventTypes.HandleHasNewFormat));
            Assert.That(rmStream[1].Handle, Is.EqualTo("rev_1"));
            Assert.That(rmStream[1].FormatInfo.DocumentFormat.ToString(), Is.EqualTo("blah"));
            Assert.That(rmStream[1].FormatInfo.BlobId.ToString(), Is.EqualTo("test"));
            Assert.That(rmStream[1].FormatInfo.PipelineId.ToString(), Is.EqualTo("tika"));
            Assert.That(rmStream[1].Filename.FileName, Is.EqualTo("test")); //expectation returns always the same handle
            Assert.That(rmStream[1].Filename.Extension, Is.EqualTo("txt"));
        }
    }
}
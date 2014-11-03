using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public abstract class DocumentSpecifications : AggregateSpecification<Document, DocumentState>
    {
        protected static readonly DocumentId _id = new DocumentId(1);
        protected static readonly FileId _fileId = new FileId("newFile");
        protected static readonly DocumentHandle Handle = new DocumentHandle("handle-to-file");
        protected static readonly FileNameWithExtension _fname = new FileNameWithExtension("pathTo.file");
        protected static readonly DocumentHandleInfo _handleInfo = new DocumentHandleInfo(Handle, _fname);

        protected static Document Document
        {
            get { return Aggregate; }
        }
    }

    public class when_a_document_is_created : DocumentSpecifications
    {
        Establish context = () => Create();

        Because of = () => Document.Create(_id, _fileId, _handleInfo);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It DocumentId_should_be_assigned = () =>
            Document.Id.ShouldBeTheSameAs(_id);

        It created_event_should_store_relevant_info = () =>
        {
            var e = RaisedEvent<DocumentCreated>();
            e.FileId.ShouldEqual(_fileId);
        };

        It DocumentHandleAttachedEvent_should_have_been_raised = ()=>
            EventHasBeenRaised<DocumentHandleAttached>().ShouldBeTrue();
        
        It DocumentHandleAttachedEvent_should_store_relevant_info = () =>
        {
            var e = RaisedEvent<DocumentHandleAttached>();
            e.HandleInfo.ShouldEqual(_handleInfo);
        };
    }

    public class when_a_document_is_created_twice : DocumentSpecifications
    {
        static Exception _ex;

        Establish context = () =>
        {
            Create();
            Document.Create(_id, _fileId, _handleInfo);
        };

        Because of = () => _ex = Catch.Exception(() => Document.Create(_id, _fileId, _handleInfo));

        It a_domain_exception_should_be_thrown = () =>
        {
            _ex.ShouldNotBeNull();
            _ex.ShouldBeAssignableTo<DomainException>();
            _ex.ShouldContainErrorMessage("Already created");
        };
    }

    public class when_a_format_is_added_to_a_document : DocumentSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormat = new DocumentFormat("xml");
        protected static readonly FileId XmlFileId = new FileId("xml");
        protected static readonly PipelineId XmlPiplePipelineId = new PipelineId("xml");

        Establish context = () => SetUp(new DocumentState() { });

        Because of = () => Document.AddFormat(XmlDocumentFormat, XmlFileId, XmlPiplePipelineId);

        It FormatAddedToDocument_event_should_have_been_raised = () =>
            EventHasBeenRaised<FormatAddedToDocument>().ShouldBeTrue();

        It event_should_have_file_and_format_id = () =>
        {
            var e = RaisedEvent<FormatAddedToDocument>();
            e.FileId.ShouldBeTheSameAs(XmlFileId);
            e.DocumentFormat.ShouldBeTheSameAs(XmlDocumentFormat);
        };

        It state_should_have_xml_format = () =>
            State.HasFormat(new DocumentFormat("XML")).ShouldBeTrue();
    }

    [Subject("document with xml format")]
    public class document_with_xml_format : DocumentSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");
        protected static readonly FileId xmlFileId1 = new FileId("xml1");

        protected static readonly DocumentFormat XmlDocumentFormatId2 = new DocumentFormat("xml");
        protected static readonly FileId xmlFileId2 = new FileId("xml1");
        protected static readonly PipelineId XmlPiplePipelineId = new PipelineId("xml");

        public class when_xml_format_is_added : document_with_xml_format
        {
            Establish context = () => SetUp(new DocumentState(
                new KeyValuePair<DocumentFormat, FileId>(XmlDocumentFormatId1, xmlFileId1))
                );

            Because of = () => Document.AddFormat(XmlDocumentFormatId2, xmlFileId2, XmlPiplePipelineId);

            It DocumentFormatHasBeenUpdated_event_should_have_been_raised = () =>
                EventHasBeenRaised<DocumentFormatHasBeenUpdated>().ShouldBeTrue();

            It event_should_have_file_and_format_id = () =>
            {
                var e = RaisedEvent<DocumentFormatHasBeenUpdated>();
                e.FileId.ShouldBeTheSameAs(xmlFileId2);
                e.DocumentFormat.ShouldBeTheSameAs(XmlDocumentFormatId2);
            };
        }
    }

    [Subject("DocumentFormats")]
    public class when_document_format_has_been_deleted : DocumentSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");
        protected static readonly FileId xmlFileId1 = new FileId("xml1");

        Establish context =
            () => SetUp(new DocumentState(new KeyValuePair<DocumentFormat, FileId>(XmlDocumentFormatId1, xmlFileId1)));

        Because of = () => Document.DeleteFormat(XmlDocumentFormatId1);

        It DocumentFormatHasBeenDeleted_event_should_have_been_raised =
            () => EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeTrue();

        It document_format_do_not_contain_deleted_format =
            () => Aggregate.InternalState.Formats.ContainsKey(XmlDocumentFormatId1).ShouldBeFalse();

        It document_format_do_not_contain_fileId =
            () => Aggregate.InternalState.Formats.Values.ShouldNotContain(xmlFileId1);
    }

    [Subject("DocumentFormats")]
    public class when_document_format_deleted_will_be_deleted : DocumentSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");

        Establish context =
            () => SetUp(new DocumentState());

        Because of = () => Document.DeleteFormat(XmlDocumentFormatId1);

        It DocumentFormatHasBeenDeleted_event_should_not_been_raised =
            () => EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeFalse();
    }

    [Subject("Document created")]
    public class when_a_document_is_deleted : DocumentSpecifications
    {
        Establish context = () =>
        {
            Create();
            Document.Create(_id, _fileId, _handleInfo);
        };

        Because of = () =>
        {
            Document.Delete(Handle);
        };

        It DocumentDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_been_raised = () => 
            EventHasBeenRaised<DocumentHandleDetached>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_correct_handle = () =>
            RaisedEvent<DocumentHandleDetached>().Handle.ShouldBeLike(Handle);

        It Internal_state_should_not_track_old_handle = () => {
            State.IsValidHandle(Handle).ShouldBeTrue();
            State.HandleCount(Handle).ShouldBeLike(0);
        };
    }
    [Subject("With a New Created Document")]
    public class when_trying_to_delete_with_a_wrong_handle: DocumentSpecifications
    {
        Establish context = () =>
        {
            Create();
            Document.Create(_id, _fileId, _handleInfo);
        };

        Because of = () =>
        {
            exception = Catch.Exception(() => Document.Delete(new DocumentHandle("not_in_this_doc")));
        };

        It Exception_should_have_been_raised = () =>
            exception.ShouldNotBeNull();
            
        static Exception exception;
    }

    [Subject("DocumentEvents")]
    public class when_a_deduplicated_document_is_deleted : DocumentSpecifications
    {
        private static readonly DocumentHandle _otherHandle = new DocumentHandle("other");
        private static readonly DocumentHandleInfo _otherInfo = new DocumentHandleInfo(
            new DocumentHandle("other"),
            new FileNameWithExtension("a.file")
        );
        Establish context = () => Create();

        Because of = () =>
        {
            Document.Create(_id, _fileId, _handleInfo);
            Document.Deduplicate(
                new DocumentId(2), 
                _otherInfo
            );
            Document.Delete(Handle);
        };

        It DocumentDeleted_event_should_not_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeFalse();

        It DocumentHandleDetached_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentHandleDetached>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_correct_handle = () =>
            RaisedEvent<DocumentHandleDetached>().Handle.ShouldBeLike(Handle);

        It Internal_state_should_not_track_old_handle = () =>
        {
            State.IsValidHandle(Handle).ShouldBeTrue();
            State.HandleCount(Handle).ShouldBeLike(0);
        };

        It Internal_state_should_track_other_handle = () =>
            State.IsValidHandle(_otherHandle).ShouldBeTrue();
    }

    [Subject("Document")]
    public class when_a_document_is_deleted_with_wrong_handle : DocumentSpecifications
    {
        private static Exception Exception { get; set; }
        Establish context = () => Create();

        Because of = () =>
        {
            Document.Create(_id, _fileId, _handleInfo);
            Exception = Catch.Exception(() => Document.Delete(new DocumentHandle("not_this_one")));
        };


        It a_domainException_should_have_been_raised = () =>
        {
            Assert.NotNull(Exception);
            Assert.IsTrue(Exception is DomainException);
        };
    }
    [Subject("Document with an handle assigned twice")]
    public class when_deleting_an_handle : DocumentSpecifications
    {
        private static Exception Exception { get; set; }
        private Establish context = () =>
        {
            var state = new DocumentState();
            state.Handles.Add(new DocumentHandle("h"),2);
            SetUp(state);
        };

        Because of = () =>
        {
            Document.Delete(new DocumentHandle("h"));
        };

        It state_should_track_handle = () =>
        {
            State.Handles.Count.ShouldBeLike(1);
        };
    }

    [Subject("with a document")]
    public class when_a_document_is_deduplicated : DocumentSpecifications
    {
        static readonly DocumentId _otherDocumentId = new DocumentId("Document_2");
        static readonly DocumentHandleInfo _otherHandleInfo = new DocumentHandleInfo(
            new DocumentHandle("other_handle"),
            new FileNameWithExtension("Another.document")
        );

        Establish context = () => SetUp(new DocumentState());

        Because of = () => Document.Deduplicate(_otherDocumentId, _otherHandleInfo);

        It DocumentHasBeenDeduplicated_event_should_be_raised = () =>
            EventHasBeenRaised<DocumentHasBeenDeduplicated>().ShouldBeTrue();

        It DocumentHandleAttached_event_should_be_raised = () =>
            EventHasBeenRaised<DocumentHandleAttached>().ShouldBeTrue();

        It DocumentHasBeenDeduplicated_event_should_have_documentId_and_handle = () =>
        {
            var e = RaisedEvent<DocumentHasBeenDeduplicated>();
            Assert.AreSame(_otherDocumentId, e.OtherDocumentId);
            Assert.AreSame(_otherHandleInfo.Handle, e.Handle);
        };

        It DocumentHandleAttached_event_should_have_handle_and_fileName = () =>
        {
            var e = RaisedEvent<DocumentHandleAttached>();
            Assert.AreSame(_otherHandleInfo, e.HandleInfo);
        };
    }
}

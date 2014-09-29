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

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public abstract class DocumentSpecifications : AggregateSpecification<Document, DocumentState>
    {
        protected static readonly DocumentId _id = new DocumentId(1);
        protected static readonly FileId _fileId = new FileId("newFile");
        protected static Document Document
        {
            get { return Aggregate; }
        }
    }

    public class when_a_document_is_created : DocumentSpecifications
    {
        Establish context = () => Create();

        Because of = () => Document.Create(_id,_fileId);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It DocumentId_should_be_assigned = () =>
            Document.Id.ShouldBeTheSameAs(_id);
    }

    public class when_a_document_is_created_twice : DocumentSpecifications
    {
        private static Exception _ex;
        private Establish context = () =>
        {
            Create();
            Document.Create(_id, _fileId);
        };
        
        Because of = () => _ex = Catch.Exception(()=> Document.Create(_id, _fileId));

        private It a_domain_exception_should_be_thrown = () =>
        {
            _ex.ShouldNotBeNull();
            _ex.ShouldBeAssignableTo<DomainException>();
            _ex.ShouldContainErrorMessage("Already created");
        };
    }

    public class when_a_format_is_added_to_a_document : DocumentSpecifications
    {
        protected static readonly FormatValue XmlFormatValue = new FormatValue("xml");
        protected static readonly FileId XmlFileId = new FileId("xml");

        Establish context = () => SetUp(new DocumentState() { });

        Because of = () => Document.AddFormat(XmlFormatValue, XmlFileId);

        It FormatAddedToDocument_event_should_have_been_raised = () =>
            EventHasBeenRaised<FormatAddedToDocument>().ShouldBeTrue();

        It event_should_have_file_and_format_id = () =>
        {
            var e = RaisedEvent<FormatAddedToDocument>();
            e.FileId.ShouldBeTheSameAs(XmlFileId);
            e.FormatValue.ShouldBeTheSameAs(XmlFormatValue);
        };

        It state_should_have_xml_format = () =>
            State.HasFormat(new FormatValue("XML")).ShouldBeTrue();
    }

    [Subject("document with xml format")]
    public class document_with_xml_format : DocumentSpecifications
    {
        protected static readonly FormatValue xmlFormatId1 = new FormatValue("xml");
        protected static readonly FileId xmlFileId1 = new FileId("xml1");

        protected static readonly FormatValue xmlFormatId2 = new FormatValue("xml");
        protected static readonly FileId xmlFileId2 = new FileId("xml1");

        public class when_xml_format_is_added : document_with_xml_format
        {
            Establish context = () => SetUp(new DocumentState(
                new KeyValuePair<FormatValue, FileId>(xmlFormatId1, xmlFileId1))
            );

            Because of = () => Document.AddFormat(xmlFormatId2, xmlFileId2);

            It DocumentFormatHasBeenUpdated_event_should_have_been_raised = () =>
                EventHasBeenRaised<DocumentFormatHasBeenUpdated>().ShouldBeTrue();

            It event_should_have_file_and_format_id = () =>
            {
                var e = RaisedEvent<DocumentFormatHasBeenUpdated>();
                e.FileId.ShouldBeTheSameAs(xmlFileId2);
                e.FormatValue.ShouldBeTheSameAs(xmlFormatId2);
            };
        }
    }

    [Subject("DocumentFormats")]
    public class when_document_format_has_been_deleted:DocumentSpecifications
    {
        protected static readonly FormatValue xmlFormatId1 = new FormatValue("xml");
        protected static readonly FileId xmlFileId1 = new FileId("xml1");

        private Establish context =
            () => SetUp(new DocumentState(new KeyValuePair<FormatValue, FileId>(xmlFormatId1, xmlFileId1)));

        private Because of = () => Document.DeleteFormat(xmlFormatId1);

        private It DocumentFormatHasBeenDeleted_event_should_have_been_raised =
            () => EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeTrue();

        private It document_format_do_not_contain_deleted_format =
            () => Aggregate.InternalState.Formats.ContainsKey(xmlFormatId1).ShouldBeFalse();

        private It document_format_do_not_contain_fileId =
            () => Aggregate.InternalState.Formats.Values.ShouldNotContain(xmlFileId1);
    }

    [Subject("DocumentFormats")]
    public class when_document_format_deleted_will_be_deleted : DocumentSpecifications
    {
        protected static readonly FormatValue xmlFormatId1 = new FormatValue("xml");
        private Establish context =
            () => SetUp(new DocumentState());

        private Because of = () => Document.DeleteFormat(xmlFormatId1);

        private It DocumentFormatHasBeenDeleted_event_should_not_been_raised =
            () => EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeFalse();
    }

    [Subject("DocumentEvents")]
    public class when_a_document_is_deleted : DocumentSpecifications
    {
        private Establish context = () => Create();

        private Because of = () =>
            {
                Document.Create(_id,_fileId);
                Document.Delete();
            };

        private It DocumentDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();
    }
}

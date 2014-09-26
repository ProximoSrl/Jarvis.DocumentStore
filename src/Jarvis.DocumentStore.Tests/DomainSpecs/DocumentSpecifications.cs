using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public abstract class DocumentSpecifications : AggregateSpecification<Document, DocumentState>
    {
        protected static readonly DocumentId _id = new DocumentId(1);
        protected static Document Document {
            get { return Aggregate; }
        }
    }

    public class when_a_document_is_created : DocumentSpecifications
    {
        Establish context = () => Create();

        Because of = () => Document.Create(_id);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It DocumentId_should_be_assigned = () =>
            Document.Id.ShouldBeTheSameAs(_id);
    }

    public class when_a_format_is_added_to_a_document : DocumentSpecifications
    {
        protected static readonly FormatId xmlFormatId = new FormatId("xml");
        protected static readonly FileId xmlFileId = new FileId("xml");
        
        Establish context = () => SetUp(new DocumentState(){});

        Because of = () => Document.AddFormat(xmlFormatId, xmlFileId);

        It FormatAddedToDocument_event_should_have_been_raised = () =>
            EventHasBeenRaised<FormatAddedToDocument>().ShouldBeTrue();

        It event_should_have_file_and_format_id = () =>
        {
            var e = RaisedEvent<FormatAddedToDocument>();
            e.FileId.ShouldBeTheSameAs(xmlFileId);
            e.FormatId.ShouldBeTheSameAs(xmlFormatId);
        };

        It state_should_have_xml_format = () =>
            State.HasFormat(new FormatId("XML")).ShouldBeTrue();
    }

    [Subject("document with xml format")]
    public class document_with_xml_format : DocumentSpecifications
    {
        protected static readonly FormatId xmlFormatId1 = new FormatId("xml");
        protected static readonly FileId xmlFileId1 = new FileId("xml1");

        protected static readonly FormatId xmlFormatId2 = new FormatId("xml");
        protected static readonly FileId xmlFileId2 = new FileId("xml1");

        public class when_xml_format_is_added : document_with_xml_format
        {
            Establish context = () => SetUp(new DocumentState(
                new KeyValuePair<FormatId,FileId>(xmlFormatId1, xmlFileId1))
            );

            Because of = () => Document.AddFormat(xmlFormatId2, xmlFileId2);

            It DocumentFormatHasBeenUpdated_event_should_have_been_raised = () =>
                EventHasBeenRaised<DocumentFormatHasBeenUpdated>().ShouldBeTrue();

            It event_should_have_file_and_format_id = () =>
            {
                var e = RaisedEvent<DocumentFormatHasBeenUpdated>();
                e.FileId.ShouldBeTheSameAs(xmlFileId2);
                e.FormatId.ShouldBeTheSameAs(xmlFormatId2);
            };
        }
    }
}

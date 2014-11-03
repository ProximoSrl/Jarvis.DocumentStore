using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public class when_a_format_is_added_to_a_document : DocumentSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormat = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId = new BlobId("xml");
        protected static readonly PipelineId XmlPiplePipelineId = new PipelineId("xml");

        Establish context = () => SetUp(new DocumentState() { });

        Because of = () => Document.AddFormat(XmlDocumentFormat, XmlBlobId, XmlPiplePipelineId);

        It FormatAddedToDocument_event_should_have_been_raised = () =>
            EventHasBeenRaised<FormatAddedToDocument>().ShouldBeTrue();

        It event_should_have_file_and_format_id = () =>
        {
            var e = RaisedEvent<FormatAddedToDocument>();
            e.BlobId.ShouldBeTheSameAs(XmlBlobId);
            e.DocumentFormat.ShouldBeTheSameAs(XmlDocumentFormat);
        };

        It state_should_have_xml_format = () =>
            State.HasFormat(new DocumentFormat("XML")).ShouldBeTrue();
    }
}
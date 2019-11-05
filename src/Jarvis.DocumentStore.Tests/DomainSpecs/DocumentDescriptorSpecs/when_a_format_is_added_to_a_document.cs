using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.DocumentStore.Tests.Support;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    public class when_a_format_is_added_to_a_documentDescriptor : DocumentDescriptorSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormat = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId = new BlobId("xml");
        protected static readonly PipelineId XmlPiplePipelineId = new PipelineId("xml");

        Establish context = () => SetUp(new DocumentDescriptorState(),_id);

        Because of = () => DocumentDescriptor.AddFormat(XmlDocumentFormat, XmlBlobId, XmlPiplePipelineId);

        It FormatAddedToDocumentDescriptor_event_should_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<FormatAddedToDocumentDescriptor>().ShouldBeTrue();

        It event_should_have_file_and_format_id = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<FormatAddedToDocumentDescriptor>();
            e.BlobId.ShouldBeTheSameAs(XmlBlobId);
            e.DocumentFormat.ShouldBeTheSameAs(XmlDocumentFormat);
        };

        It state_should_have_xml_format = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.State.HasFormat(new DocumentFormat("XML")).ShouldBeTrue();
    }
}
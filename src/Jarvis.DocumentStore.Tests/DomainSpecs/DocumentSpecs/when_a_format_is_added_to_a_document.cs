using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public class when_a_format_is_added_to_a_documentDescriptor : DocumentDescriptorSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormat = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId = new BlobId("xml");
        protected static readonly PipelineId XmlPiplePipelineId = new PipelineId("xml");

        Establish context = () => AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.SetUp(new DocumentDescriptorState() { });

        Because of = () => DocumentDescriptor.AddFormat(XmlDocumentFormat, XmlBlobId, XmlPiplePipelineId);

        It FormatAddedToDocumentDescriptor_event_should_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<FormatAddedToDocumentDescriptor>().ShouldBeTrue();

        It event_should_have_file_and_format_id = () =>
        {
            var e = AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<FormatAddedToDocumentDescriptor>();
            e.BlobId.ShouldBeTheSameAs(XmlBlobId);
            e.DocumentFormat.ShouldBeTheSameAs(XmlDocumentFormat);
        };

        It state_should_have_xml_format = () =>
            AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.State.HasFormat(new DocumentFormat("XML")).ShouldBeTrue();
    }
}
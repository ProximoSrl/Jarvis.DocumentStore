using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    public class when_a_documentDescriptor_is_initialized : DocumentDescriptorSpecifications
    {
        Establish context = () => AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create(_id);

        Because of = () => DocumentDescriptor.Initialize(_blobId, _handleInfo, _fileHash, _fileName);

        It DocumentDescriptorCreatedEvent_should_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentDescriptorInitialized>().ShouldBeTrue();

        It DocumentAddedToFormat_should_not_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<FormatAddedToDocumentDescriptor>().ShouldBeFalse();

        It created_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<DocumentDescriptorInitialized>();
            e.BlobId.ShouldEqual(_blobId);
        };
    }
}
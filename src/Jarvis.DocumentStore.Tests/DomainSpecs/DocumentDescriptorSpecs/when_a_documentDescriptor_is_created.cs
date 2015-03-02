using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    public class when_a_documentDescriptor_is_created : DocumentDescriptorSpecifications
    {
        Establish context = () => AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create(_id);

        Because of = () => DocumentDescriptor.Create(_blobId, _handleInfo, _fileHash, _fileName);

        It DocumentDescriptorCreatedEvent_should_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentDescriptorCreated>().ShouldBeTrue();

        It created_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<DocumentDescriptorCreated>();
            e.BlobId.ShouldEqual(_blobId);
        };
    }
}
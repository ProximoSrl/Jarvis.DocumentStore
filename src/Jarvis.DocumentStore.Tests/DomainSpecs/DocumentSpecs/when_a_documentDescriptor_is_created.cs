using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public class when_a_documentDescriptor_is_created : DocumentDescriptorSpecifications
    {
        Establish context = () => AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create();

        Because of = () => DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);

        It DocumentDescriptorCreatedEvent_should_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentDescriptorCreated>().ShouldBeTrue();

        It DocumentDescriptorId_should_be_assigned = () =>
            ShouldExtensionMethods.ShouldBeTheSameAs(DocumentDescriptor.Id, _id);

        It created_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<DocumentDescriptorCreated>();
            e.BlobId.ShouldEqual(_blobId);
        };
    }
}
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("Document created")]
    public class when_a_documentDescriptor_is_deleted : DocumentDescriptorSpecifications
    {
        Establish context = () =>
        {
            Create(_id);
            DocumentDescriptor.Initialize(_blobId, _handleInfo, _fileHash, _fileName);
            DocumentDescriptor.Create(Handle);
        };

        Because of = () => { DocumentDescriptor.Delete(Handle); };

        It DocumentDescriptorDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDescriptorDeleted>().ShouldBeTrue();
    }
}
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("Document created")]
    public class when_a_documentDescriptor_is_deleted : DocumentDescriptorSpecifications
    {
        Establish context = () =>
        {
            Create();
            DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);
            DocumentDescriptor.Process(Handle);
        };

        Because of = () => { DocumentDescriptor.Delete(Handle); };

        It DocumentDescriptorDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDescriptorDeleted>().ShouldBeTrue();
    }
}
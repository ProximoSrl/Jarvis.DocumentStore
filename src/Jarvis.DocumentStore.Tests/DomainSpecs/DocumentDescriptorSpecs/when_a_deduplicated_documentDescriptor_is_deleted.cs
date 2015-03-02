using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("DocumentEvents")]
    public class when_a_deduplicated_documentDescriptor_is_deleted : DocumentDescriptorSpecifications
    {
        static readonly DocumentHandle _otherHandle = new DocumentHandle("other");

        Establish context = () =>
        {
            Create(_id);
            DocumentDescriptor.Create(_blobId, _handleInfo, _fileHash, _fileName);
            DocumentDescriptor.Process(Handle);
            DocumentDescriptor.Deduplicate(new DocumentDescriptorId(2), _otherHandle, _fname);
        };

        Because of = () =>
        {
            DocumentDescriptor.Delete(Handle);
        };

        It DocumentDeleted_event_should_not_have_been_raised = () =>
            EventHasBeenRaised<DocumentDescriptorDeleted>().ShouldBeFalse();
    }
}
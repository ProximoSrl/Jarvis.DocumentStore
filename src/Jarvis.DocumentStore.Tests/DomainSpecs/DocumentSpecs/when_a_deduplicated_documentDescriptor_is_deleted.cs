using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("DocumentEvents")]
    public class when_a_deduplicated_documentDescriptor_is_deleted : DocumentDescriptorSpecifications
    {
        static readonly DocumentHandle _otherHandle = new DocumentHandle("other");

        Establish context = () =>
        {
            Create();
            DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);
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
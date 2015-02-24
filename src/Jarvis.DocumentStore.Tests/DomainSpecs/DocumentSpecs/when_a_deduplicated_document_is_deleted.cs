using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("DocumentEvents")]
    public class when_a_deduplicated_document_is_deleted : DocumentSpecifications
    {
        static readonly DocumentHandle _otherHandle = new DocumentHandle("other");

        Establish context = () =>
        {
            Create();
            Document.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);
            Document.Process(Handle);
            Document.Deduplicate(new DocumentId(2), _otherHandle, _fname);
        };

        Because of = () =>
        {
            Document.Delete(Handle);
        };

        It DocumentDeleted_event_should_not_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeFalse();
    }
}
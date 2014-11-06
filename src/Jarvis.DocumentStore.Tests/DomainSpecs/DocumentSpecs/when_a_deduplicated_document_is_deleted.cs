using CQRS.TestHelpers;
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

        static readonly DocumentHandleInfo _otherInfo = new DocumentHandleInfo(
            new DocumentHandle("other"),
            new FileNameWithExtension("a.file")
            );

        It DocumentDeleted_event_should_not_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeFalse();

        Establish context = () => Create();

        Because of = () =>
        {
            Document.Create(_id, _blobId, _handleInfo);
            Document.Deduplicate(new DocumentId(2),_otherInfo.Handle);
            Document.Delete(Handle);
        };
    }
}
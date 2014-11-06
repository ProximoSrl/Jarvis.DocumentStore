using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("Document created")]
    public class when_a_document_is_deleted : DocumentSpecifications
    {
        It DocumentDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>()
                .ShouldBeTrue();

        It Internal_state_should_not_track_old_handle = () =>
        {
            State.IsValidHandle(Handle)
                .ShouldBeTrue();
            State.HandleCount(Handle)
                .ShouldBeLike(0);
        };

        Establish context = () =>
        {
            Create();
            Document.Create(_id, _blobId, _handleInfo);
        };

        Because of = () => { Document.Delete(Handle); };
    }
}
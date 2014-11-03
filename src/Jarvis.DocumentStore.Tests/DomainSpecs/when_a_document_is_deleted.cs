using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [Subject("Document created")]
    public class when_a_document_is_deleted : DocumentSpecifications
    {
        Establish context = () =>
        {
            Create();
            Document.Create(_id, _blobId, _handleInfo);
        };

        Because of = () =>
        {
            Document.Delete(Handle);
        };

        It DocumentDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_been_raised = () => 
            EventHasBeenRaised<DocumentHandleDetached>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_correct_handle = () =>
            RaisedEvent<DocumentHandleDetached>().Handle.ShouldBeLike(Handle);

        It Internal_state_should_not_track_old_handle = () => {
                                                                  State.IsValidHandle(Handle).ShouldBeTrue();
                                                                  State.HandleCount(Handle).ShouldBeLike(0);
        };
    }
}
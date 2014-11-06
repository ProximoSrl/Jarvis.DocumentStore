using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("Document created")]
    public class when_a_document_is_deleted : DocumentSpecifications
    {
        Establish context = () =>
        {
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.Create();
            Document.Create(_id, _blobId, _handleInfo);
        };

        Because of = () =>
        {
            Document.Delete(Handle);
        };

        It DocumentDeleted_event_should_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_been_raised = () => 
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentHandleDetached>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_correct_handle = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.RaisedEvent<DocumentHandleDetached>().Handle.ShouldBeLike(Handle);

        It Internal_state_should_not_track_old_handle = () => {
                                                                  AggregateSpecification<Core.Domain.Document.Document, DocumentState>.State.IsValidHandle(Handle).ShouldBeTrue();
                                                                  AggregateSpecification<Core.Domain.Document.Document, DocumentState>.State.HandleCount(Handle).ShouldBeLike(0);
        };
    }
}
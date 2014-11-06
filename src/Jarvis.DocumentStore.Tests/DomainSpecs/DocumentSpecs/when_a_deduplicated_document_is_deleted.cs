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
        private static readonly DocumentHandle _otherHandle = new DocumentHandle("other");
        private static readonly DocumentHandleInfo _otherInfo = new DocumentHandleInfo(
            new DocumentHandle("other"),
            new FileNameWithExtension("a.file")
            );
        Establish context = () => AggregateSpecification<Core.Domain.Document.Document, DocumentState>.Create();

        Because of = () =>
        {
            Document.Create(_id, _blobId, _handleInfo);
            Document.Deduplicate(
                new DocumentId(2), 
                _otherInfo
                );
            Document.Delete(Handle);
        };

        It DocumentDeleted_event_should_not_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentDeleted>().ShouldBeFalse();

        It DocumentHandleDetached_event_should_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentHandleDetached>().ShouldBeTrue();

        It DocumentHandleDetached_event_should_have_correct_handle = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.RaisedEvent<DocumentHandleDetached>().Handle.ShouldBeLike(Handle);

        It Internal_state_should_not_track_old_handle = () =>
        {
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.State.IsValidHandle(Handle).ShouldBeTrue();
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.State.HandleCount(Handle).ShouldBeLike(0);
        };

        It Internal_state_should_track_other_handle = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.State.IsValidHandle(_otherHandle).ShouldBeTrue();
    }
}
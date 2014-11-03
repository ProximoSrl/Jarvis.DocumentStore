using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public class when_a_document_is_created : DocumentSpecifications
    {
        Establish context = () => Create();

        Because of = () => Document.Create(_id, _blobId, _handleInfo);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It DocumentId_should_be_assigned = () =>
            Document.Id.ShouldBeTheSameAs(_id);

        It created_event_should_store_relevant_info = () =>
        {
            var e = RaisedEvent<DocumentCreated>();
            e.BlobId.ShouldEqual(_blobId);
        };

        It DocumentHandleAttachedEvent_should_have_been_raised = ()=>
            EventHasBeenRaised<DocumentHandleAttached>().ShouldBeTrue();
        
        It DocumentHandleAttachedEvent_should_store_relevant_info = () =>
        {
            var e = RaisedEvent<DocumentHandleAttached>();
            e.HandleInfo.ShouldEqual(_handleInfo);
        };
    }
}
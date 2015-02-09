using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public class when_a_document_is_created : DocumentSpecifications
    {
        Establish context = () => AggregateSpecification<Core.Domain.Document.Document, DocumentState>.Create();

        Because of = () => Document.Create(_id, _blobId, _handleInfo,_fileHash);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It DocumentId_should_be_assigned = () =>
            ShouldExtensionMethods.ShouldBeTheSameAs(Document.Id, _id);

        It created_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<Core.Domain.Document.Document, DocumentState>.RaisedEvent<DocumentCreated>();
            e.BlobId.ShouldEqual(_blobId);
        };
    }
}
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
            Create();
            Document.Create(_id, _blobId, _handleInfo, _fileHash);
            Document.Process(Handle);
        };

        Because of = () => { Document.Delete(Handle); };

        It DocumentDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();
    }
}
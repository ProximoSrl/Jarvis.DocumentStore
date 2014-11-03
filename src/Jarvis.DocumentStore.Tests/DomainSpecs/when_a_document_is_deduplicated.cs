using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [Subject("with a document")]
    public class when_a_document_is_deduplicated : DocumentSpecifications
    {
        static readonly DocumentId _otherDocumentId = new DocumentId("Document_2");
        static readonly DocumentHandleInfo _otherHandleInfo = new DocumentHandleInfo(
            new DocumentHandle("other_handle"),
            new FileNameWithExtension("Another.document")
            );

        Establish context = () => SetUp(new DocumentState());

        Because of = () => Document.Deduplicate(_otherDocumentId, _otherHandleInfo);

        It DocumentHasBeenDeduplicated_event_should_be_raised = () =>
            EventHasBeenRaised<DocumentHasBeenDeduplicated>().ShouldBeTrue();

        It DocumentHandleAttached_event_should_be_raised = () =>
            EventHasBeenRaised<DocumentHandleAttached>().ShouldBeTrue();

        It DocumentHasBeenDeduplicated_event_should_have_documentId_and_handle = () =>
        {
            var e = RaisedEvent<DocumentHasBeenDeduplicated>();
            Assert.AreSame(_otherDocumentId, e.OtherDocumentId);
            Assert.AreSame(_otherHandleInfo.Handle, e.Handle);
        };

        It DocumentHandleAttached_event_should_have_handle_and_fileName = () =>
        {
            var e = RaisedEvent<DocumentHandleAttached>();
            Assert.AreSame(_otherHandleInfo, e.HandleInfo);
        };
    }
}
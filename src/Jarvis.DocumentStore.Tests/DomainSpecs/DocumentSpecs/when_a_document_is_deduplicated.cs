using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("with a document")]
    public class when_a_document_is_deduplicated : DocumentSpecifications
    {
        static readonly DocumentId _otherDocumentId = new DocumentId("Document_2");
        static readonly DocumentHandleInfo _otherHandleInfo = new DocumentHandleInfo(
            new DocumentHandle("other_handle"),
            new FileNameWithExtension("Another.document")
            );

        Establish context = () => AggregateSpecification<Core.Domain.Document.Document, DocumentState>.SetUp(new DocumentState());

        Because of = () => Document.Deduplicate(_otherDocumentId, _otherHandleInfo.Handle);

        It DocumentHasBeenDeduplicated_event_should_be_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentHasBeenDeduplicated>().ShouldBeTrue();

        It DocumentHasBeenDeduplicated_event_should_have_documentId_and_handle = () =>
        {
            var e = AggregateSpecification<Core.Domain.Document.Document, DocumentState>.RaisedEvent<DocumentHasBeenDeduplicated>();
            Assert.AreSame(_otherDocumentId, e.OtherDocumentId);
            Assert.AreSame(_otherHandleInfo.Handle, e.Handle);
        };
    }
}
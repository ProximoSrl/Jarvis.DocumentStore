using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;
using NSubstitute;
using System;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public class when_a_document_is_created_with_known_format : DocumentSpecifications
    {
        Establish context = () =>
        {
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.Create();
            Document.DocumentFormatTranslator.GetFormatFromFileName(Arg.Any<String>()).Returns(new DocumentFormat("pdf"));
        };

        Because of = () => Document.Create(_id, _blobId, _handleInfo,_fileHash, _fileName);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It FormatAddedToDocumentEvent_should_have_been_raised = () =>
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.EventHasBeenRaised<FormatAddedToDocument>().ShouldBeTrue();
        
        It format_added_to_document_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<Core.Domain.Document.Document, DocumentState>.RaisedEvent<FormatAddedToDocument>();
            e.BlobId.ShouldEqual(_blobId);
            e.DocumentFormat.ShouldEqual(new DocumentFormat("pdf"));
            e.CreatedBy.ShouldEqual(null);
        };
    }
}
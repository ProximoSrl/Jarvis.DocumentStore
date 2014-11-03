using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [Subject("DocumentFormats")]
    public class when_document_format_deleted_will_be_deleted : DocumentSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");

        Establish context =
            () => SetUp(new DocumentState());

        Because of = () => Document.DeleteFormat(XmlDocumentFormatId1);

        It DocumentFormatHasBeenDeleted_event_should_not_been_raised =
            () => EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeFalse();
    }
}
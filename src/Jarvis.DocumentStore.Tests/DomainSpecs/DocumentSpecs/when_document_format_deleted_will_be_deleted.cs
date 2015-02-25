using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("DocumentFormats")]
    public class when_document_format_deleted_will_be_deleted : DocumentDescriptorSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");

        Establish context =
            () => AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.SetUp(new DocumentDescriptorState());

        Because of = () => DocumentDescriptor.DeleteFormat(XmlDocumentFormatId1);

        It DocumentFormatHasBeenDeleted_event_should_not_been_raised =
            () => AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeFalse();
    }
}
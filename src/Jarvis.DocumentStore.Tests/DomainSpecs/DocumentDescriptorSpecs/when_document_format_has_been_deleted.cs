using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("DocumentFormats")]
    public class when_document_format_has_been_deleted : DocumentDescriptorSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId1 = new BlobId("xml1");

        Establish context =() => SetUp(
            new DocumentDescriptorState(new KeyValuePair<DocumentFormat, BlobId>(XmlDocumentFormatId1, XmlBlobId1)),
            _id
        );

        Because of = () => DocumentDescriptor.DeleteFormat(XmlDocumentFormatId1);

        It DocumentFormatHasBeenDeleted_event_should_have_been_raised =
            () => AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentFormatHasBeenDeleted>().ShouldBeTrue();

        It document_format_do_not_contain_deleted_format =
            () => State.Formats.ContainsKey(XmlDocumentFormatId1).ShouldBeFalse();

        It document_format_do_not_contain_blobId =
            () => State.Formats.Values.ShouldNotContain(XmlBlobId1);
    }
}
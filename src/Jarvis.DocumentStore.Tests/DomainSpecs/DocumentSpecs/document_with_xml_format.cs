using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("document with xml format")]
    public class documentDescriptor_with_xml_format : DocumentDescriptorSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId1 = new BlobId("xml1");

        protected static readonly DocumentFormat XmlDocumentFormatId2 = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId2 = new BlobId("xml1");
        protected static readonly PipelineId XmlPiplePipelineId = new PipelineId("xml");

        public class when_xml_format_is_added : documentDescriptor_with_xml_format
        {
            Establish context = () => SetUp(new DocumentDescriptorState(
                new KeyValuePair<DocumentFormat, BlobId>(XmlDocumentFormatId1, XmlBlobId1))
                );

            Because of = () => DocumentDescriptor.AddFormat(XmlDocumentFormatId2, XmlBlobId2, XmlPiplePipelineId);

            It DocumentFormatHasBeenUpdated_event_should_have_been_raised = () =>
                EventHasBeenRaised<DocumentFormatHasBeenUpdated>().ShouldBeTrue();

            It event_should_have_file_and_format_id = () =>
            {
                var e = RaisedEvent<DocumentFormatHasBeenUpdated>();
                e.BlobId.ShouldBeTheSameAs(XmlBlobId2);
                e.DocumentFormat.ShouldBeTheSameAs(XmlDocumentFormatId2);
            };
        }
    }
}
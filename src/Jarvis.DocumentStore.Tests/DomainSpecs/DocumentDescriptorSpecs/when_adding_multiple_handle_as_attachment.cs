using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("attachments")]
    public class when_adding_multiple_handle_as_attachment : DocumentDescriptorSpecifications
    {
        protected static readonly DocumentFormat XmlDocumentFormatId1 = new DocumentFormat("xml");
        protected static readonly BlobId XmlBlobId1 = new BlobId("xml1");

        Establish context = () =>
        {
            SetUp(
                new DocumentDescriptorState(new KeyValuePair<DocumentFormat, BlobId>(XmlDocumentFormatId1, XmlBlobId1)),
                _id
            );
            State.AddAttachment(_attachmentDocumentHandle);
        };

        Because of = () => DocumentDescriptor.AddAttachment(_attachmentDocumentHandle, "path.xml");
  

        It document_has_new_attachment_should_not_be_raised = () =>
            EventHasBeenRaised<DocumentDescriptorHasNewAttachment>().ShouldBeFalse();

        It state_should_contains_only_one_instance_of_attachment = () =>
           State.Attachments.ShouldContainOnly(_attachmentDocumentHandle);
    }
}

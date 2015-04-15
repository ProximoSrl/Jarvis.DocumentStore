using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("Document created")]
    public class when_a_documentDescriptor_is_deleted_with_attachments : DocumentDescriptorSpecifications
    {
        Establish context = () =>
        {
            Create(_id);
            DocumentDescriptor.Initialize(_blobId, _handleInfo, _fileHash, _fileName);
            DocumentDescriptor.Create(_handleInfo);
            DocumentDescriptor.AddAttachment(_attachmentDocumentHandle, "Test.txt");
        };

        Because of = () => { DocumentDescriptor.Delete(Handle); };

        It DocumentDescriptorDeleted_event_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentDescriptorDeleted>().ShouldBeTrue();


        It deleted_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>
                .RaisedEvent<DocumentDescriptorDeleted>();
            e.Attachments.Length.ShouldEqual(1);
            e.Attachments[0].ShouldEqual(_attachmentDocumentHandle);
        };
    }
}
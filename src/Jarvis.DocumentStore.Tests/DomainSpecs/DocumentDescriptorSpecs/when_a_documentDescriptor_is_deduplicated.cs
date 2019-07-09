using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.DocumentStore.Tests.Support;
using Machine.Specifications;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("with a document")]
    public class when_a_documentDescriptor_is_deduplicated : DocumentDescriptorSpecifications
    {
        static readonly DocumentDescriptorId _otherDocumentId = new DocumentDescriptorId(2);
        static readonly DocumentHandleInfo _otherHandleInfo = new DocumentHandleInfo(
            new DocumentHandle("other_handle"),
            new FileNameWithExtension("Another.document")
            );

        Establish context = () => SetUp(new DocumentDescriptorState(),_id);

        Because of = () => DocumentDescriptor.Deduplicate(_otherDocumentId, _otherHandleInfo);

        It DocumentDescriptorHasBeenDeduplicated_event_should_be_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentDescriptorHasBeenDeduplicated>().ShouldBeTrue();

        It DocumentDescriptorHasBeenDeduplicated_event_should_have_documentId_and_handle = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<DocumentDescriptorHasBeenDeduplicated>();
            Assert.AreSame(_otherDocumentId, e.DuplicatedDocumentDescriptorId);
            Assert.AreSame(_otherHandleInfo, e.HandleInfo);
        };
    }
}
using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;
using NSubstitute;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    public class when_a_documentDescriptor_is_initialized_with_known_format : DocumentDescriptorSpecifications
    {

        Establish context = () =>
        {
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create(_id);
            DocumentDescriptor.DocumentFormatTranslator.GetFormatFromFileName(Arg.Any<String>()).Returns(new DocumentFormat("pdf"));
        };

        Because of = () => DocumentDescriptor.Initialize(_blobId, _handleInfo,_fileHash, _fileName);

        It DocumentDescriptorCreatedEvent_should_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<DocumentDescriptorInitialized>().ShouldBeTrue();

        It FormatAddedToDocumentDescriptorEvent_should_have_been_raised = () =>
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.EventHasBeenRaised<FormatAddedToDocumentDescriptor>().ShouldBeTrue();

        It format_added_to_documentDescriptor_event_should_store_relevant_info = () =>
        {
            var e = AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.RaisedEvent<FormatAddedToDocumentDescriptor>();
            e.BlobId.ShouldEqual(_blobId);
            e.DocumentFormat.ShouldEqual(new DocumentFormat("pdf"));
            e.CreatedBy.ShouldEqual(null);
        };
    }

   
}
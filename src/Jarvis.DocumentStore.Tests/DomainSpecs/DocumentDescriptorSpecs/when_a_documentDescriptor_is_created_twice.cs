using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;
using NSubstitute;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    public class when_a_documentDescriptor_is_created_twice : DocumentDescriptorSpecifications
    {
        private static Exception exception;
        Establish context = () =>
        {
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create(_id);
            State.Apply(new DocumentDescriptorInitialized(_blobId, _handleInfo, _fileHash));
            State.Apply(new DocumentDescriptorCreated(_blobId, _handleInfo.Handle));
            DocumentDescriptor.DocumentFormatTranslator.GetFormatFromFileName(Arg.Any<String>()).Returns(new DocumentFormat("pdf"));
        };

        Because of = () =>
        {
            exception = Catch.Exception(() => DocumentDescriptor.Create(_handleInfo.Handle));
        };

        It Exception_should_have_been_raised = () => exception.ShouldNotBeNull();
            

    }
}
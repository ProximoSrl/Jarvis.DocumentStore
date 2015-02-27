using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("With a New Created Document")]
    public class when_trying_to_delete_with_a_wrong_handle: DocumentDescriptorSpecifications
    {
        Establish context = () =>
        {
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create();
            DocumentDescriptor.Create(_blobId, _handleInfo, _fileHash, _fileName);
        };

        Because of = () =>
        {
            exception = Catch.Exception(() => DocumentDescriptor.Delete(new DocumentHandle("not_in_this_doc")));
        };

        It Exception_should_have_been_raised = () =>
            exception.ShouldNotBeNull();
            
        static Exception exception;
    }
}
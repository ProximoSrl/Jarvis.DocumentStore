using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("With a New Created Document")]
    public class when_trying_to_delete_with_a_wrong_handle: DocumentDescriptorSpecifications
    {
        Establish context = () =>
        {
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create();
            DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);
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
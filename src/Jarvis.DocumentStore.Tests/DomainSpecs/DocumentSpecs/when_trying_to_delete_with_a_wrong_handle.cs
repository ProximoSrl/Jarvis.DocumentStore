using System;
using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("With a New Created Document")]
    public class when_trying_to_delete_with_a_wrong_handle: DocumentSpecifications
    {
        Establish context = () =>
        {
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.Create();
            Document.Create(_id, _blobId, _handleInfo,_fileHash);
        };

        Because of = () =>
        {
            exception = Catch.Exception(() => Document.Delete(new DocumentHandle("not_in_this_doc")));
        };

        It Exception_should_have_been_raised = () =>
            exception.ShouldNotBeNull();
            
        static Exception exception;
    }
}
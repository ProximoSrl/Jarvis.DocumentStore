using System;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [Subject("With a New Created Document")]
    public class when_trying_to_delete_with_a_wrong_handle: DocumentSpecifications
    {
        Establish context = () =>
        {
            Create();
            Document.Create(_id, _blobId, _handleInfo);
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
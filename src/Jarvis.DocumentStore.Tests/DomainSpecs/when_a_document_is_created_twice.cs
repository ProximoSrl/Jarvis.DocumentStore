using System;
using CQRS.Kernel.Engine;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public class when_a_document_is_created_twice : DocumentSpecifications
    {
        static Exception _ex;

        Establish context = () =>
        {
            Create();
            Document.Create(_id, _blobId, _handleInfo);
        };

        Because of = () => _ex = Catch.Exception(() => Document.Create(_id, _blobId, _handleInfo));

        It a_domain_exception_should_be_thrown = () =>
        {
            _ex.ShouldNotBeNull();
            _ex.ShouldBeAssignableTo<DomainException>();
            _ex.ShouldContainErrorMessage("Already created");
        };
    }
}
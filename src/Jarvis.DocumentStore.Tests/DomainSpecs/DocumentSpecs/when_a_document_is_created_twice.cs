using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.Framework.TestHelpers;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public class when_a_document_is_created_twice : DocumentSpecifications
    {
        static Exception _ex;

        Establish context = () =>
        {
            AggregateSpecification<Core.Domain.Document.Document, DocumentState>.Create();
            Document.Create(_id, _blobId, _handleInfo,_fileHash);
        };

        Because of = () => _ex = Catch.Exception(() => Document.Create(_id, _blobId, _handleInfo,_fileHash));

        It a_domain_exception_should_be_thrown = () =>
        {
            _ex.ShouldNotBeNull();
            _ex.ShouldBeAssignableTo<DomainException>();
            _ex.ShouldContainErrorMessage("Already created");
        };
    }
}
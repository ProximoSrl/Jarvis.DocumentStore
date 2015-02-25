using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.Framework.TestHelpers;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public class when_a_documentDescriptor_is_created_twice : DocumentDescriptorSpecifications
    {
        static Exception _ex;

        Establish context = () =>
        {
            AggregateSpecification<Core.Domain.Document.DocumentDescriptor, DocumentDescriptorState>.Create();
            DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);
        };

        Because of = () => _ex = Catch.Exception(() => DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName));

        It a_domain_exception_should_be_thrown = () =>
        {
            _ex.ShouldNotBeNull();
            _ex.ShouldBeAssignableTo<DomainException>();
            _ex.ShouldContainErrorMessage("Already created");
        };
    }
}
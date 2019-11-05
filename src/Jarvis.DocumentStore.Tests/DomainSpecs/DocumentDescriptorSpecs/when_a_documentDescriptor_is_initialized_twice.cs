using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    public class when_a_documentDescriptor_is_initialized_twice : DocumentDescriptorSpecifications
    {
        static Exception _ex;

        Establish context = () =>
        {
            AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create(_id);
            DocumentDescriptor.Initialize(_blobId, _handleInfo, _fileHash, _fileName);
        };

        Because of = () => _ex = Catch.Exception(() => DocumentDescriptor.Initialize(_blobId, _handleInfo, _fileHash, _fileName));

        It a_domain_exception_should_be_thrown = () =>
        {
            _ex.ShouldNotBeNull();
            _ex.ShouldBeAssignableTo<DomainException>();
            _ex.ShouldContainErrorMessage("Already initialized");
        };
    }
}
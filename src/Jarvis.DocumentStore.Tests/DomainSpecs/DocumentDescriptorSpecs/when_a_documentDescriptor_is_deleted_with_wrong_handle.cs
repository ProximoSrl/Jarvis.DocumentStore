using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs;
using Jarvis.Framework.TestHelpers;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Machine.Specifications;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentDescriptorSpecs
{
    [Subject("Document")]
    public class when_a_documentDescriptor_is_deleted_with_wrong_handle : DocumentDescriptorSpecifications
    {
        private static Exception Exception { get; set; }
        Establish context = () => AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create();

        Because of = () =>
        {
            DocumentDescriptor.Create(_blobId, _handleInfo, _fileHash, _fileName);
            Exception = Catch.Exception(() => DocumentDescriptor.Delete(new DocumentHandle("not_this_one")));
        };


        It a_domainException_should_have_been_raised = () =>
        {
            Assert.NotNull(Exception);
            Assert.IsTrue(Exception is DomainException);
        };
    }
}
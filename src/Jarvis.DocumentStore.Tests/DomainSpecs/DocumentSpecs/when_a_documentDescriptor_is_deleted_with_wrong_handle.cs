using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.Framework.TestHelpers;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Machine.Specifications;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    [Subject("Document")]
    public class when_a_documentDescriptor_is_deleted_with_wrong_handle : DocumentDescriptorSpecifications
    {
        private static Exception Exception { get; set; }
        Establish context = () => AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>.Create();

        Because of = () =>
        {
            DocumentDescriptor.Create(_id, _blobId, _handleInfo, _fileHash, _fileName);
            Exception = Catch.Exception(() => DocumentDescriptor.Delete(new DocumentHandle("not_this_one")));
        };


        It a_domainException_should_have_been_raised = () =>
        {
            Assert.NotNull(Exception);
            Assert.IsTrue(Exception is DomainException);
        };
    }
}
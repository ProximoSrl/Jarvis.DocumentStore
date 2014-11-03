using System;
using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [Subject("Document")]
    public class when_a_document_is_deleted_with_wrong_handle : DocumentSpecifications
    {
        private static Exception Exception { get; set; }
        Establish context = () => Create();

        Because of = () =>
        {
            Document.Create(_id, _blobId, _handleInfo);
            Exception = Catch.Exception(() => Document.Delete(new DocumentHandle("not_this_one")));
        };


        It a_domainException_should_have_been_raised = () =>
        {
            Assert.NotNull(Exception);
            Assert.IsTrue(Exception is DomainException);
        };
    }
}
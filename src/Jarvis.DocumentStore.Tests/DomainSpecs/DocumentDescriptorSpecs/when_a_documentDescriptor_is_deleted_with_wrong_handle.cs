using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
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
        Establish context = () =>
        {
            Create(_id);
            DocumentDescriptor.Initialize(_blobId, _handleInfo, _fileHash, _fileName);
        };

        Because of = () =>
        {
            DocumentDescriptor.Delete(new DocumentHandle("not_this_one"));
        };

        It no_event_should_be_raised = () =>
        {
            EventHasBeenRaised<DocumentHandleDetached>().ShouldBeFalse();
        };
    }
}
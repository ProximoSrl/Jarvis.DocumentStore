using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs.HandleSpecs
{
    public abstract class HandleSpecification : AggregateSpecification<Handle, HandleState>
    {
        public static readonly HandleId HandleId = new HandleId(1);
        public static readonly DocumentId LinkedDocumentId = new DocumentId(1);
        public static readonly DocumentHandle DocumentHandle = new DocumentHandle("this_is_an_handle");
        public static Handle Handle { get { return Aggregate; }}
    }

    [Subject("with a uninitialized handle")]
    public class when_creating_an_handle : HandleSpecification
    {
        Establish context = () => Create();
        Because of = () => Handle.Initialize(HandleId, DocumentHandle);

        It handle_initilized_event_should_be_raised = () =>
            EventHasBeenRaised<HandleInitialized>().ShouldBeTrue();

        It handle_initialized_event_should_have_id_and_handle = () =>
        {
            var e = RaisedEvent<HandleInitialized>();
            e.Handle.ShouldBeLike(DocumentHandle);
            e.Id.ShouldBeLike(HandleId);
        };
    }

    public abstract class with_an_initialized_handle : HandleSpecification
    {
        Establish context = () => SetUp(new HandleState(HandleId, Handle));
    }

    [Subject(typeof(with_an_initialized_handle))]
    public class when_link_a_document : with_an_initialized_handle
    {
        Because of = () => Handle.Link(LinkedDocumentId);
        
        It handleLinkedEvent_should_be_raised = () =>
            EventHasBeenRaised<HandleLinked>().ShouldBeTrue();

        It event_should_have_document_id = () =>
            RaisedEvent<HandleLinked>().DocumentId.ShouldBeLike(LinkedDocumentId);
    }
}

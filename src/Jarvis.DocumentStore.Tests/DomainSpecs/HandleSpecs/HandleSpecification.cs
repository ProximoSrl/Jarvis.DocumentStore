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
        public static readonly DocumentId Document_1 = new DocumentId(1);
        public static readonly DocumentId Document_2 = new DocumentId(2);
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
        Because of = () => Handle.Link(Document_1);
        
        It handleLinkedEvent_should_be_raised = () =>
            EventHasBeenRaised<HandleLinked>().ShouldBeTrue();

        It event_should_have_document_id = () =>
            RaisedEvent<HandleLinked>().DocumentId.ShouldBeLike(Document_1);
    }

    public class with_an_handle_linked_to_document_1 : with_an_initialized_handle
    {
        Establish context = () =>
        {
            var handleState = new HandleState(HandleId, Handle);
            handleState.Link(Document_1);
            SetUp(handleState);
        };
    }

    [Subject("with a handle linked to Document_1")]
    public class when_i_link_document_1_again : with_an_handle_linked_to_document_1
    {
        Because of = () => Handle.Link(Document_1);

        It handleLinkedEvent_should_not_be_raised = () =>
            EventHasBeenRaised<HandleLinked>().ShouldBeFalse();
    }

    [Subject("with a handle linked to Document_1")]
    public class when_i_delete_the_handle: with_an_handle_linked_to_document_1
    {
        Because of = () => Handle.Delete();

        It HandleDeletedEvent_should_be_raised = () =>
            EventHasBeenRaised<HandleDeleted>().ShouldBeTrue();

        It State_should_track_deletion = () =>
            State.HasBeenDeleted.ShouldBeTrue();
    }

    [Subject("with a deleted handle")]
    public class when_trying_to_delete_again : with_an_initialized_handle
    {
        Establish context = () => {
            State.MarkAsDeleted();
        };
        Because of = () => Handle.Delete();

        It HandleDeletedEvent_should_be_raised = () =>
            EventHasBeenRaised<HandleDeleted>().ShouldBeFalse();
    }
}

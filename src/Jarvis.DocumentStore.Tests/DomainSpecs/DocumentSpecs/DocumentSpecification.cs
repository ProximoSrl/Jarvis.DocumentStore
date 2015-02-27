using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.TestHelpers;
using Machine.Specifications;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public abstract class DocumentSpecification : AggregateSpecification<Document, DocumentState>
    {
        public static readonly DocumentId DocumentId1 = new DocumentId(1);
   
        public static readonly DocumentDescriptorId Document_1 = new DocumentDescriptorId(1);
        public static readonly DocumentDescriptorId Document_2 = new DocumentDescriptorId(2);
        public static readonly FileNameWithExtension FileName_1 = new FileNameWithExtension("a","file");
        public static readonly DocumentCustomData CustomData_1 = new DocumentCustomData();
        public static readonly DocumentCustomData CustomData_2 = new DocumentCustomData();
        public static readonly DocumentCustomData CustomData_3 = new DocumentCustomData(){{"a", "b"}};
        public static readonly DocumentHandle DocumentHandle = new DocumentHandle("this_is_an_document");
        public static readonly DocumentHandle AttachmentDocumentHandle = new DocumentHandle("this_is_the_attachment");
        public static readonly DocumentHandle OtherAttachmentDocumentHandle = new DocumentHandle("this_is_the_other_attachment");

        public static Document DocumentAggregate { get { return Aggregate; }}
    }

    [Subject("with a uninitialized document")]
    public class WhenCreatingAnDocument : DocumentSpecification
    {
        Establish context = () => Create();
        Because of = () => DocumentAggregate.Initialize(DocumentId1, DocumentHandle);

        It document_initilized_event_should_be_raised = () =>
            EventHasBeenRaised<DocumentInitialized>().ShouldBeTrue();

        It document_initialized_event_should_have_id_and_handle = () =>
        {
            var e = RaisedEvent<DocumentInitialized>();
            e.Handle.ShouldBeTheSameAs(DocumentHandle);
            e.Id.ShouldBeLike(DocumentId1);
        };

        It linked_document_should_be_null = () =>
            State.LinkedDocument.ShouldBeNull();
    }

   

    public abstract class WithAnInitializedDocument : DocumentSpecification
    {
        Establish context = () => SetUp(new DocumentState(DocumentId1, DocumentHandle),DocumentId1);
    }

    [Subject(typeof(WithAnInitializedDocument))]
    public class when_link_a_document : WithAnInitializedDocument
    {
        Because of = () => DocumentAggregate.Link(Document_1);

        It linked_document_should_document_1 = () => {
            State.LinkedDocument.ShouldNotBeNull();
            State.LinkedDocument.ShouldBeLike(Document_1);
        };

        It documentLinkedEvent_should_be_raised = () =>
            EventHasBeenRaised<DocumentLinked>().ShouldBeTrue();

        It event_should_have_document_id = () =>
            RaisedEvent<DocumentLinked>().DocumentId.ShouldBeLike(Document_1);

        It event_should_have_old_document_id = () =>
            RaisedEvent<DocumentLinked>().PreviousDocumentId.ShouldBeNull();
    }

    [Subject("Attachments")]
    public class WhenAddingAnAttachToDocument : WithAnInitializedDocument
    {
        Because of = () => DocumentAggregate.AddAttachment(AttachmentDocumentHandle);

        It document_has_new_attachment_should_be_raised = () =>
            EventHasBeenRaised<DocumentHasNewAttachment>().ShouldBeTrue();

        It document_has_new_attachment_should_contains_attachment = () =>
        {
            var e = RaisedEvent<DocumentHasNewAttachment>();
            e.Attachment.ShouldBeTheSameAs(AttachmentDocumentHandle);
            e.Handle.ShouldBeTheSameAs(DocumentHandle);
        };

        It state_should_contains_new_attachment = () =>
            State.Attachments.ShouldContainOnly(AttachmentDocumentHandle);
    }

    [Subject("Attachments")]
    public class WhenAddingSameDocumentMultipleTimeAsAttachment : WithAnInitializedDocument
    {
        Establish context = () =>
        {
            var state = new DocumentState(DocumentId1, DocumentHandle);
            state.AddAttachment(AttachmentDocumentHandle);
            SetUp(state, DocumentId1);
        };

        Because of = () => DocumentAggregate.AddAttachment(AttachmentDocumentHandle);

        It document_has_new_attachment_should_not_be_raised = () =>
            EventHasBeenRaised<DocumentHasNewAttachment>().ShouldBeFalse();

        It state_should_contains_only_one_instance_of_attachment = () =>
           State.Attachments.ShouldContainOnly(AttachmentDocumentHandle);
    }

    [Subject("with an document wihout file name")]
    public class when_filename_is_set : WithAnInitializedDocument
    {
        Because of = () => 
            DocumentAggregate.SetFileName(new FileNameWithExtension("a","file"));
        
        It filename_should_be_set = () => 
            State.FileName.ShouldBeLike(new FileNameWithExtension("a","file"));
        
        It filename_set_event_should_have_been_raised = () => 
            EventHasBeenRaised<DocumentFileNameSet>().ShouldBeTrue();
    }

    [Subject("with an document with file name")]
    public class when_filename_is_set_twice : WithAnInitializedDocument
    {
        Establish context = () => 
            State.SetFileName(new FileNameWithExtension("a", "file"));
        
        Because of = () => 
            DocumentAggregate.SetFileName(new FileNameWithExtension("a","file"));
        
        It filename_should_be_set = () => 
            State.FileName.ShouldBeLike(new FileNameWithExtension("a","file"));
        
        It filename_set_event_should_have_not_been_raised = () =>
            EventHasBeenRaised<DocumentFileNameSet>().ShouldBeFalse();
    }

    public class WithAnDocumentLinkedToDocument1 : WithAnInitializedDocument
    {
        Establish context = () =>
        {
            var handleState = new DocumentState(DocumentId1, DocumentHandle);
            handleState.Link(Document_1);
            SetUp(handleState, DocumentId1);
        };
    }

    [Subject("with a document linked to Document_1")]
    public class when_i_link_document_1_again : WithAnDocumentLinkedToDocument1
    {
        Because of = () => DocumentAggregate.Link(Document_1);

        It documentLinkedEvent_should_not_be_raised = () =>
            EventHasBeenRaised<DocumentLinked>().ShouldBeFalse();
    }

    [Subject("with a document linked to Document_1")]
    public class when_i_link_document_2_again : WithAnDocumentLinkedToDocument1
    {
        Because of = () => DocumentAggregate.Link(Document_2);

        It documentLinkedEvent_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentLinked>().ShouldBeTrue();

        It documentLinkedEvent_should_have_old_document_set_to_document_1 = () =>
            RaisedEvent<DocumentLinked>().PreviousDocumentId.ShouldBeLike(Document_1);

        It documentLinkedEvent_should_have_new_document_set_to_document_2 = () =>
            RaisedEvent<DocumentLinked>().DocumentId.ShouldBeLike(Document_2);
    }

    [Subject("with a document linked to Document_1")]
    public class WhenIDeleteTheDocument: WithAnDocumentLinkedToDocument1
    {
        Because of = () => DocumentAggregate.Delete();

        It HandleDeletedEvent_should_be_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();

        It State_should_track_deletion = () =>
            State.HasBeenDeleted.ShouldBeTrue();
    }

    [Subject("with a deleted document")]
    public class when_trying_to_delete_again : WithAnInitializedDocument
    {
        Establish context = () => {
            State.MarkAsDeleted();
        };
        Because of = () => DocumentAggregate.Delete();

        It HandleDeletedEvent_should_be_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeFalse();
    }

    [Subject("with a deleted document with attach")]
    public class when_trying_to_delete_document_with_attachments : WithAnDocumentLinkedToDocument1
    {
        Establish context = () =>
        {
            State.AddAttachment(AttachmentDocumentHandle);
            State.AddAttachment(OtherAttachmentDocumentHandle);
        };

        Because of = () => DocumentAggregate.Delete();

        It HandleDeletedEvent_should_be_raised = () =>
            EventHasBeenRaised<DocumentDeleted>().ShouldBeTrue();
        It AttachmentDeletedEvent_should_be_raised_for_child = () =>
           AllRaisedEvents<AttachmentDeleted>().Count().ShouldEqual(2);
    }

    [Subject("can delete an attachment")]
    public class when_deleting_an_attachment : WithAnDocumentLinkedToDocument1
    {
        Establish context = () =>
        {
            State.AddAttachment(AttachmentDocumentHandle);
            State.AddAttachment(OtherAttachmentDocumentHandle);
        };

        Because of = () => DocumentAggregate.DeleteAttachment(OtherAttachmentDocumentHandle);

        It AttachmentDeletedEvent_should_be_raised = () =>
            EventHasBeenRaised<AttachmentDeleted>().ShouldBeTrue();


        It attachmentDeletedEvent_should_have_right_handle = () =>
        {
            var e = RaisedEvent<AttachmentDeleted>();
            e.Handle.ShouldBeTheSameAs(OtherAttachmentDocumentHandle);
        };

        private It State_should_not_have_attachment_deleted = () =>
        {
            State.Attachments.ShouldNotContain(OtherAttachmentDocumentHandle);
            State.Attachments.ShouldContain(AttachmentDocumentHandle);
        };
    }

    [Subject("with an initialized document")]
    public class when_customData_are_set : WithAnInitializedDocument
    {
        Because of = () => DocumentAggregate.SetCustomData(CustomData_1);

        It CustomDataSetEvent_have_beed_raised = () => 
            EventHasBeenRaised<DocumentCustomDataSet>().ShouldBeTrue();

        It CustomDataSetEvent_should_have_data_set = () =>
        {
            var e = RaisedEvent<DocumentCustomDataSet>();
            e.CustomData.ShouldNotBeNull();
            e.CustomData.ShouldBeLike(CustomData_1);
        };

        It State_should_track_latest_customData = ()=>
            State.CustomData.ShouldBeLike(CustomData_1);
    }

    public abstract class with_custom_data_set_to_custom_data_1 : WithAnInitializedDocument
    {
        Establish context = () => State.SetCustomData(CustomData_1);
    }

    [Subject("with an document with CustomData_1")]
    public class when_trying_to_set_custom_data_2: with_custom_data_set_to_custom_data_1
    {
        Because of = () => DocumentAggregate.SetCustomData(CustomData_2);
    
        It CustomDataSetEvent_should_not_have_beed_raised = () =>
           EventHasBeenRaised<DocumentCustomDataSet>().ShouldBeFalse();
    }

    [Subject("with an document with CustomData_1")]
    public class when_trying_to_set_custom_data_3: with_custom_data_set_to_custom_data_1
    {
        Because of = () => DocumentAggregate.SetCustomData(CustomData_3);
    
        It CustomDataSetEvent_should_have_beed_raised = () =>
           EventHasBeenRaised<DocumentCustomDataSet>().ShouldBeTrue();
    }

    [Subject("with an initialized document")]
    public class when_trying_to_set_null_custom_data : WithAnInitializedDocument
    {
        Because of = () => DocumentAggregate.SetCustomData(null);
        It CustomDataSetEvent_should_not_have_beed_raised = () =>
          EventHasBeenRaised<DocumentCustomDataSet>().ShouldBeFalse();
    }

    [Subject("with an document with CustomData_1")]
    public class when_trying_to_unset_custom_data : with_custom_data_set_to_custom_data_1
    {
        Because of = () => DocumentAggregate.SetCustomData(null);

        It CustomDataSetEvent_should_have_beed_raised = () =>
           EventHasBeenRaised<DocumentCustomDataSet>().ShouldBeTrue();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.TestHelpers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Machine.Specifications;

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    public abstract class DocumentSpecifications : AggregateSpecification<Document, DocumentState>
    {
        protected static readonly DocumentId _id = new DocumentId(1);
        protected static Document Document {
            get { return Aggregate; }
        }
    }

    public class when_a_document_is_created : DocumentSpecifications
    {
        Establish context = () => Create();

        Because of = () => Document.Create(_id);

        It DocumentCreatedEvent_should_have_been_raised = () =>
            EventHasBeenRaised<DocumentCreated>().ShouldBeTrue();

        It DocumentId_should_be_assigned = () =>
            Document.Id.ShouldBeLike(_id);
    }

    public class when_a_document_is_ : DocumentSpecifications
    {
        Establish context = () => SetUp(new DocumentState()
        {
            
        });

//        Because of = () => { Document.AddFormat() };
    }
}

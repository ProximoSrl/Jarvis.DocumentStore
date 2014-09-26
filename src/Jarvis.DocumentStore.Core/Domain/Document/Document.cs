using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.Events;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.ValueObjects;
using Jarvis.DocumentStore.Core.Model;
using Microsoft.SqlServer.Server;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class Document : AggregateRoot<DocumentState>
    {
        public Document(DocumentState initialState)
            : base(initialState)
        {
        }

        public Document()
        {
        }

        public void Create(DocumentId id)
        {
            RaiseEvent(new DocumentCreated(id));
        }

        public void AddFormat(FormatId formatId, FileId fileId)
        {
            if (InternalState.HasFormat(formatId))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(formatId, fileId));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(formatId, fileId));
            }
        }
    }

    public class DocumentCreated : DomainEvent
    {
        public DocumentCreated(DocumentId id)
        {
            this.AggregateId = id;
        }
    }

    public class FormatAddedToDocument : DomainEvent
    {
        public FormatId FormatId { get; private set; }
        public FileId FileId { get; private set; }

        public FormatAddedToDocument(FormatId formatId, FileId fileId)
        {
            FormatId = formatId;
            FileId = fileId;
        }
    }

    public class DocumentFormatHasBeenUpdated : DomainEvent
    {
        public FormatId FormatId { get; private set; }
        public FileId FileId { get; private set; }

        public DocumentFormatHasBeenUpdated(FormatId formatId, FileId fileId)
        {
            FormatId = formatId;
            FileId = fileId;
        }
    }


    public class DocumentState : AggregateState
    {
        public IDictionary<FormatId, FileId> Formats { get; private set; }

        public DocumentState(params KeyValuePair<FormatId, FileId>[] formats) : this()
        {
            foreach (var keyValuePair in formats)
            {
                this.Formats.Add(keyValuePair);
            }
        }

        public DocumentState()
        {
            this.Formats = new Dictionary<FormatId, FileId>();
        }

        void When(DocumentCreated e)
        {
            this.AggregateId = e.AggregateId;
        }

        void When(FormatAddedToDocument e)
        {
            this.Formats.Add(e.FormatId, e.FileId);
        }

        public bool HasFormat(FormatId formatId)
        {
            return Formats.ContainsKey(formatId);
        }
    }

    public class DocumentId : EventStoreIdentity
    {
        public DocumentId(string id)
            : base(id)
        {
        }

        public DocumentId(long id)
            : base(id)
        {
        }
    }

    public class FormatId : LowercaseStringId
    {
        public FormatId(string id)
            : base(id)
        {
        }
    }

}

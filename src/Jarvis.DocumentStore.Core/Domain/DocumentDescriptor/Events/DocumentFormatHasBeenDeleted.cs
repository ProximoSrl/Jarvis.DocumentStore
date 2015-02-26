using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentFormatHasBeenDeleted : DomainEvent
    {
        public DocumentFormat DocumentFormat { get; private set; }

        public DocumentFormatHasBeenDeleted(DocumentFormat documentFormat)
        {
            DocumentFormat = documentFormat;
        }
    }
}
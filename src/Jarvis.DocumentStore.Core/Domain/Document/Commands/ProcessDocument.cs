namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class ProcessDocument : DocumentCommand
    {
        public ProcessDocument(DocumentId aggregateId) : base(aggregateId)
        {
        }
    }
}
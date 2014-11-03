using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers
{
    public class ProcessDocumentCommandHandler : DocumentCommandHandler<ProcessDocument>
    {
        protected override void Execute(ProcessDocument cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Process()); 
        }
    }
}
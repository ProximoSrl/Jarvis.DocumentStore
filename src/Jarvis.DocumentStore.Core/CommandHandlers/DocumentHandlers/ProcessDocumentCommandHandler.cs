using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class ProcessDocumentCommandHandler : DocumentCommandHandler<ProcessDocumentDescriptor>
    {
        protected override void Execute(ProcessDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Process(cmd.Handle)); 
        }
    }
}
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class ProcessDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<ProcessDocumentDescriptor>
    {
        protected override void Execute(ProcessDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Process(cmd.Handle)); 
        }
    }
}
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class AddFormatToDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<AddFormatToDocumentDescriptor>
    {
        protected override void Execute(AddFormatToDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.AddFormat(cmd.DocumentFormat, cmd.BlobId, cmd.CreatedBy));
        }
    }
}

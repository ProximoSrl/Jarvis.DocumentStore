using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class AddFormatToDocumentCommandHandler : DocumentCommandHandler<AddFormatToDocumentDescriptor>
    {
        protected override void Execute(AddFormatToDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.AddFormat(cmd.DocumentFormat, cmd.BlobId, cmd.CreatedBy));
        }
    }
}

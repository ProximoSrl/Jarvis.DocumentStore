using Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
{
    public class CreateAttachmentCommandHandler : DocumentDescriptorCommandHandler<CreateAttachment>
    {
        readonly IHandleMapper _mapper;

        public CreateAttachmentCommandHandler(IHandleMapper mapper, IDocumentWriter writer)
        {
            _mapper = mapper;
        }

        protected override void Execute(CreateAttachment cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => {
                    doc.AddAttachment(cmd.Handle, cmd.AttachmentPath);
                },
                true
            );

           
        }

    }
}
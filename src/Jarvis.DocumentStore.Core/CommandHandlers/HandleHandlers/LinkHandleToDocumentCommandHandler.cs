using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    public class LinkHandleToDocumentCommandHandler : RepositoryCommandHandler<Document, LinkDocumentToDocumentDescriptor>
    {
        readonly IHandleMapper _mapper;

        public LinkHandleToDocumentCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        protected override void Execute(LinkDocumentToDocumentDescriptor cmd)
        {
            var handleId = _mapper.Map(cmd.Handle);
            FindAndModify(
                handleId,
                h =>
                {
                    if (!h.HasBeenCreated) h.Initialize(handleId, cmd.Handle);
                    h.Link(cmd.DocumentDescriptorId);
                },
                true);
        }
    }

    public class DeleteDocumentCommandHandler : RepositoryCommandHandler<Document, DeleteDocument>
    {
        readonly IHandleMapper _mapper;

        public DeleteDocumentCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        protected override void Execute(DeleteDocument cmd)
        {
            var handleId = _mapper.Map(cmd.Handle);
            FindAndModify(handleId, h => h.Delete());
        }
    }
}

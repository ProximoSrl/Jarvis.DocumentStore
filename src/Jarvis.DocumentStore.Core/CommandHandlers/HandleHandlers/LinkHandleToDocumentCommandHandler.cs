using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Domain.Handle.Commands;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    public class LinkHandleToDocumentCommandHandler : RepositoryCommandHandler<Handle, LinkHandleToDocument>
    {
        readonly IHandleMapper _mapper;

        public LinkHandleToDocumentCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        protected override void Execute(LinkHandleToDocument cmd)
        {
            var handleId = _mapper.Map(cmd.Handle);
            FindAndModify(handleId, h => h.Link(cmd.DocumentId));
        }
    }

    public class DeleteHandleCommandHandler : RepositoryCommandHandler<Handle, DeleteHandle>
    {
        readonly IHandleMapper _mapper;

        public DeleteHandleCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        protected override void Execute(DeleteHandle cmd)
        {
            var handleId = _mapper.Map(cmd.Handle);
            FindAndModify(handleId, h => h.Delete());
        }
    }
}

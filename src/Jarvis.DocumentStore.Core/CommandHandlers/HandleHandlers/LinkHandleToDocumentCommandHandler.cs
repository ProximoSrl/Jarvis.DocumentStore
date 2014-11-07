using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Commands;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Domain.Handle.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    public class LinkHandleToDocumentCommandHandler : RepositoryCommandHandler<Handle, LinkHandleToDocument>
    {
        IHandleMapper _mapper;

        public LinkHandleToDocumentCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        protected override void Execute(LinkHandleToDocument cmd)
        {
            var handleId = _mapper.Map(cmd.Handle);
            FindAndModify(handleId, h => h.Link(cmd.DocumentId, cmd.FileName));

        }
    }
}

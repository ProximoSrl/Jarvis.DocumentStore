using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
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
            var handleId = _mapper.Map(cmd.HandleInfo.Handle);
            FindAndModify(
                handleId,
                h =>
                {
                    //Call Intialize on the handle
                    h.Initialize(cmd.HandleInfo.Handle);

                    h.SetFileName(cmd.HandleInfo.FileName);
                    h.SetCustomData(cmd.HandleInfo.CustomData);
                    h.Link(cmd.DocumentDescriptorId);
                },
                true);
        }
    }
}

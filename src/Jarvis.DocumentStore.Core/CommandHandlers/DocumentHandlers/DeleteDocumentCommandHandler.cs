using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
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
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class CopyDocumentCommandHandler : 
        RepositoryCommandHandler<Document, CopyDocument>
    {
        readonly IHandleMapper _mapper;

        public CopyDocumentCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// create the document with desired information.
        /// </summary>
        /// <param name="cmd"></param>
        protected override void Execute(CopyDocument cmd)
        {
            var documentId = _mapper.Map(cmd.Handle);
            FindAndModify(documentId, h => h.CopyDocument(cmd.CopiedHandle));
        }
    }
}
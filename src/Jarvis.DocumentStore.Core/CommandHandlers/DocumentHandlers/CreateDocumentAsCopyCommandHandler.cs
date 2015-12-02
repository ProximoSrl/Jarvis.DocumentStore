using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.Framework.Kernel.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class CreateDocumentAsCopyCommandHandler : RepositoryCommandHandler<Document, CreateDocumentAsCopy>
    {
        readonly IHandleMapper _mapper;

        public CreateDocumentAsCopyCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// create the document with desired information.
        /// </summary>
        /// <param name="cmd"></param>
        protected override void Execute(CreateDocumentAsCopy cmd)
        {
            var documentId = _mapper.Map(cmd.Handle);

            //this is idempotent.
            FindAndModify(
                documentId,
                h => {
                    if (!h.HasBeenCreated) h.Initialize(documentId, cmd.Handle);
                    h.SetFileName(cmd.HandleInfo.FileName);
                    h.SetCustomData(cmd.HandleInfo.CustomData);
                    h.Link(cmd.DocumentDescriptorId);
                },
                createIfNotExists: true
            );

            //Update document descriptor.
            var documentDescriptor = Repository.GetById<DocumentDescriptor>(cmd.DocumentDescriptorId);
            documentDescriptor.Attach(cmd.HandleInfo);
            Repository.Save(documentDescriptor, cmd.MessageId, d => { });
        }
    }
}
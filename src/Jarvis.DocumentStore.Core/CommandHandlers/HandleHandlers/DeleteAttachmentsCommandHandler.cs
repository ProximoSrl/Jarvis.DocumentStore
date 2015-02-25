using System;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.ReadModel;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    public class DeleteAttachmentsCommandHandler : RepositoryCommandHandler<Document, DeleteAttachments>
    {
        private readonly IDocumentWriter _documentWriter; 

        public DeleteAttachmentsCommandHandler(IDocumentWriter documentWriter)
        {
            _documentWriter = documentWriter;
        }

        protected override void Execute(DeleteAttachments cmd)
        {
            throw new NotSupportedException();
            //var  handle = _documentWriter.FindOneById(cmd.FatherHandle);
            
            //var childsOfSpecifiedSource = _documentWriter.AllSortedByHandle()
            //FindAndModify(handleId, h => h.Delete());
        }
    }
}
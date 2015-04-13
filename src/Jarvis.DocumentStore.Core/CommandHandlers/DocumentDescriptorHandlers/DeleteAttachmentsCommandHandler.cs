using System;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.ReadModel;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    //public class DeleteAttachmentsCommandHandler : RepositoryCommandHandler<Document, DeleteAttachments>
    //{
    //    private readonly IDocumentWriter _documentWriter;
    //    readonly IHandleMapper _mapper;
    //    public DeleteAttachmentsCommandHandler(IDocumentWriter documentWriter, IHandleMapper mapper)
    //    {
    //        _documentWriter = documentWriter;
    //        _mapper = mapper;
    //    }

    //    protected override void Execute(DeleteAttachments cmd)
    //    {
    //        var  handle = _documentWriter.FindOneById(cmd.FatherHandle);
    //        var allChild = handle.Attachments.ToList();
    //        var attachments = _documentWriter.AllSortedByHandle
    //            .Where(d => allChild.Any(c => c.Handle == d.Handle))
    //            .ToList();
    //        var attachmentsOfSource = attachments
    //            .Where(d => d.CustomData.Any(cd => cd.Key == "source" && cmd.Source.Equals(cd.Value) ))
    //            .Select(d => d.Handle);
    //        var fatherId = _mapper.Map(cmd.FatherHandle);
    //        var father = Repository.GetById<Document>(fatherId);
    //        foreach (var attachment in attachmentsOfSource)
    //        {
    //            father.DeleteAttachment(attachment);
    //        }
    //        Repository.Save(father, cmd.MessageId, h => { });
    //    }
    //}
}
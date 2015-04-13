using Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Jobs;
using System;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class InitializeDocumentDescriptorAsAttachCommandHandler : DocumentDescriptorCommandHandler<InitializeDocumentDescriptorAsAttach>
    {
        readonly IHandleMapper _mapper;

        public InitializeDocumentDescriptorAsAttachCommandHandler(IHandleMapper mapper, IDocumentWriter writer)
        {
            _mapper = mapper;
        }

        protected override void Execute(InitializeDocumentDescriptorAsAttach cmd)
        {
            if (cmd.HandleInfo.CustomData == null ||
                !cmd.HandleInfo.CustomData.ContainsKey(JobsConstants.AttachmentRelativePath)) 
            {
                throw new Exception(String.Format("Cannot initialize document as attach [{0}] without relative path. Missing CustomData key {1}",
                    cmd.AggregateId, JobsConstants.AttachmentRelativePath));
            }
            var path = cmd.HandleInfo.CustomData[JobsConstants.AttachmentRelativePath].ToString();
            FindAndModify(
                cmd.AggregateId,
                doc => {
                    if (!doc.HasBeenCreated)
                        doc.Initialize(cmd.BlobId, cmd.HandleInfo, cmd.Hash, cmd.FileName);
                },
                true
            );

            LinkHandleAndAddAttachment(cmd, path);
        }

        private void LinkHandleAndAddAttachment(
            InitializeDocumentDescriptorAsAttach cmd,
            String attachmentRelativePath)
        {
            var docHandle = cmd.HandleInfo.Handle;
            var id = _mapper.Map(docHandle);
            var handle = Repository.GetById<Document>(id);
            if (!handle.HasBeenCreated)
            {
                handle.Initialize(id, docHandle);
            }

            handle.SetFileName(cmd.FileName);
            handle.SetCustomData(cmd.HandleInfo.CustomData);

            Repository.Save(handle, cmd.MessageId, h => { });

            var father = Repository.GetById<DocumentDescriptor>(cmd.FatherDocumentDescriptorId);
            father.AddAttachment(cmd.HandleInfo.Handle, attachmentRelativePath);
            Repository.Save(father, cmd.MessageId, h => { });
        }

    }
}
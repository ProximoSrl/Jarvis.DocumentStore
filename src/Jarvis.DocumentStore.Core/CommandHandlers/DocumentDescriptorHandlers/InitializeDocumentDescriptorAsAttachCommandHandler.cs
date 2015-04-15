using Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Jobs;
using System;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
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
                String error =String.Format("Cannot initialize document as attach [{0}] without relative path. Missing CustomData key {1}",
                    cmd.AggregateId, JobsConstants.AttachmentRelativePath);
                Logger.Error(error);
                throw new Exception(error);
            }
            var path = cmd.HandleInfo.CustomData[JobsConstants.AttachmentRelativePath].ToString();
            FindAndModify(
                cmd.AggregateId,
                doc => {
                    if (!doc.HasBeenCreated)
                        doc.InitializeAsAttach(cmd.BlobId, cmd.HandleInfo, cmd.Hash, cmd.FileName, cmd.FatherHandle, cmd.FatherDocumentDescriptorId);
                },
                true
            );
        }

    }
}
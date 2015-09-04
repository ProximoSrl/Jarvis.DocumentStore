using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.Document.Events;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class RecycleBinProjection : AbstractProjection
        ,IEventHandler<DocumentDescriptorDeleted>
        ,IEventHandler<DocumentDeleted>
    {
        private readonly IRecycleBin _recycleBin;
        private readonly IDocumentWriter _documentWriter;
        IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentsDescriptorReader;

        public override int Priority
        {
            get
            {
                return 20; //higher priority than 
            }
        }
        public RecycleBinProjection(
            IRecycleBin recycleBin, 
            IDocumentWriter documentWriter,
            IReader<DocumentDescriptorReadModel, DocumentDescriptorId> documentsDescriptorReader)
        {
            _recycleBin = recycleBin;
            _documentWriter = documentWriter;
            _documentsDescriptorReader = documentsDescriptorReader;
        }

        public override void Drop()
        {
            _recycleBin.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentDescriptorDeleted e)
        {
            var files = e.BlobFormatsId.Concat(new []{ e.BlobId}).ToArray();
            _recycleBin.Delete(e.AggregateId, "Jarvis", e.CommitStamp, new { files });
        }

        /// <summary>
        /// Delete handle put the handle in the recycle bin
        /// </summary>
        /// <param name="e"></param>
        public void On(DocumentDeleted e)
        {
            var documentReadModel = _documentWriter.FindOneById(e.Handle);
            var data = new Dictionary<String, Object>();
            String fileName = "";
            Dictionary<String, object> customData = null;
            if (documentReadModel != null)
            {
                if (documentReadModel.FileName != null)
                {
                    fileName = documentReadModel.FileName.FileName + "." + documentReadModel.FileName.Extension;
                }
                customData = documentReadModel.CustomData;
            }
            var documentDescriptorReadModel = _documentsDescriptorReader.AllUnsorted
                .SingleOrDefault(dd => dd.Id == e.DocumentDescriptorId);
            String blobId = null;
            if (documentDescriptorReadModel != null && 
                documentDescriptorReadModel.Formats.ContainsKey(new DocumentFormat("original")))
            {
                var originalFormat = documentDescriptorReadModel.Formats[new DocumentFormat("original")];
                blobId = originalFormat.BlobId;
            }

           _recycleBin.Delete(e.AggregateId, "Jarvis", e.CommitStamp, 
               new {
                   Handle = e.Handle,
                   FileName = fileName,
                   CustomData = customData,
                   DocumentDescriptorId = e.DocumentDescriptorId,
                   OriginalBlobId = blobId,
               });
        }
    }
}

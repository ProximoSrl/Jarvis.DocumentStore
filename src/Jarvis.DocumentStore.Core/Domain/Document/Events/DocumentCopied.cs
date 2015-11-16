using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    /// <summary>
    /// Indicates that this document was copied with a new handle.
    /// </summary>
    public class DocumentCopied : DomainEvent
    {
        public DocumentCopied(
            DocumentHandle handle, 
            DocumentDescriptorId documentDescriptorId,
            DocumentHandleInfo handleInfo)
        {
            if (handle == null) throw new ArgumentNullException("handle");
            if (documentDescriptorId == null) throw new ArgumentNullException("documentDescriptorId");
            if (handleInfo == null) throw new ArgumentNullException("handleInfo");
            DocumentDescriptorId = documentDescriptorId;
            NewHandle = handle;
            HandleInfo = handleInfo;
        }

        public DocumentHandle NewHandle { get; private set; }
        public DocumentDescriptorId DocumentDescriptorId { get; private set; }

        public DocumentHandleInfo HandleInfo { get; private set; }
    }
}

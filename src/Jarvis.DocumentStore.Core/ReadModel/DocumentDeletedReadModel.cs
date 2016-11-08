using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.Framework.Shared.ReadModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentDeletedReadModel  : AbstractReadModel<String>
    {
        public DateTime DeletionDate { get; set; }
        public DocumentDescriptorId DocumentDescriptorId { get; set; }
        public DocumentHandle Handle { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Services
{
    public class DocumentMapper : AbstractIdentityTranslator<DocumentId>, IDocumentMapper
    {
        public DocumentMapper(MongoDatabase systemDB, IIdentityGenerator identityGenerator) : base(systemDB, identityGenerator)
        {
        }

        public DocumentId Map(FileId fileId)
        {
            return Translate(fileId, true);
        }
    }
}

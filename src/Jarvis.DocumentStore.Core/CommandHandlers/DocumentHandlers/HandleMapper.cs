using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.IdentitySupport;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class HandleMapper : AbstractIdentityTranslator<DocumentId>, IHandleMapper
    {
        public HandleMapper(MongoDatabase db, IIdentityGenerator identityGenerator) : base(db, identityGenerator)
        {
        }

        public void DeleteHandle(DocumentHandle handle)
        {
            var id = Translate(handle, false);
            base.DeleteAliases(id);
        }

        public DocumentId Map(DocumentHandle handle)
        {
            return Translate(handle);
        }

        public new DocumentId TryTranslate(string externalKey)
        {
            return base.TryTranslate(externalKey);
        }
    }
}

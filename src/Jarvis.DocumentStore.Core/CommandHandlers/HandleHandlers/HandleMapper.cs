﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.IdentitySupport;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    public class HandleMapper : AbstractIdentityTranslator<HandleId>, IHandleMapper
    {
        public HandleMapper(MongoDatabase db, IIdentityGenerator identityGenerator) : base(db, identityGenerator)
        {
        }

        public HandleId Map(DocumentHandle handle)
        {
            return Translate(handle);
        }
    }
}

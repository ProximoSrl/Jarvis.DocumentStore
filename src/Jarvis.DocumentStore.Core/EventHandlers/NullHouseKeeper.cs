using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.ProjectionEngine;
using NEventStore.Persistence;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class NullHouseKeeper : IHousekeeper
    {
        public void Init()
        {
            
        }

        public void RemoveAll(IPersistStreams advanced)
        {
        }
    }
}

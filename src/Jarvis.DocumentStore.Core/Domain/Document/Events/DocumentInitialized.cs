using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;
using System;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentInitialized : DomainEvent
    {

        public DocumentHandle Handle { get; private set; }

        /// <summary>
        /// True if the handle was deleted, then re-inited because it was resinserted
        /// into the system
        /// </summary>
        public Boolean ReInit { get; private set; }


        public DocumentInitialized( DocumentHandle handle, Boolean reinit)
        {
            ReInit = reinit;
            Handle = handle;
        }

    }
}
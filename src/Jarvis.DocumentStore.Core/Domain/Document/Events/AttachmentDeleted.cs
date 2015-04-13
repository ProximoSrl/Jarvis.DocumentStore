//using System;
//using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
//using Jarvis.DocumentStore.Core.Model;
//using Jarvis.Framework.Shared.Events;

//namespace Jarvis.DocumentStore.Core.Domain.Document.Events
//{
//    /// <summary>
//    /// Identify that an attachment is deleted from the father entity.
//    /// </summary>
//    public class AttachmentDeleted : DomainEvent
//    {
//        public AttachmentDeleted(DocumentHandle handle)
//        {
//            if (handle == null) throw new ArgumentNullException("handle");
            
//            Handle = handle;
//        }

//        public DocumentHandle Handle { get; private set; }
//    }
//}
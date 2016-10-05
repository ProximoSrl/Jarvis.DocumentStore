using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Model
{
    public enum HandleStreamEventTypes
    {
        Unknown = 0,
        DocumentCreated = 1,
        DocumentDeleted = 2,
        DocumentHasNewFormat = 3,
        DocumentFileNameSet = 4,
        DocumentFormatUpdated = 5,
        DocumentHasNewAttachment = 6,
        DocumentDescriptorDeleted = 7,
    }

    public static class StreamReadModelEventDataKeys
    {
        public const String ChildHandle = "ChildHandle";
    }
}

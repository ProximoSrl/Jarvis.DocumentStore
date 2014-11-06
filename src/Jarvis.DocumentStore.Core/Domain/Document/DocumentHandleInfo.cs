using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentHandleInfo
    {
        public FileNameWithExtension FileName { get; private set; }
        public HandleCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleInfo(
            DocumentHandle handle,
            FileNameWithExtension fileName,
            HandleCustomData customData = null
            )
        {
            Handle = handle;
            FileName = fileName;
            CustomData = customData;
        }
    }
}
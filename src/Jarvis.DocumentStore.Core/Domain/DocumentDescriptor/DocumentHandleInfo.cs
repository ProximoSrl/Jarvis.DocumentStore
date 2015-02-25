using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
{
    public class DocumentHandleInfo
    {
        public FileNameWithExtension FileName { get; private set; }
        public DocumentCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleInfo(
            DocumentHandle handle,
            FileNameWithExtension fileName,
            DocumentCustomData customData = null
            )
        {
            Handle = handle;
            FileName = fileName;
            CustomData = customData;
        }
    }
}
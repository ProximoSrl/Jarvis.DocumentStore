using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentHandleInfo
    {
        public FileNameWithExtension FileName { get; private set; }
        public IDictionary<string, object> CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleInfo(
            DocumentHandle handle,
            FileNameWithExtension fileName,
            IDictionary<string, object> customData = null
            )
        {
            Handle = handle;
            FileName = fileName;
            CustomData = customData;
        }
    }
}
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    /// <summary>
    /// Used by document workflow to create a new handle copy of an old
    /// handle directly initialized with a specific DocumentDescriptor.
    /// </summary>
    public class CopyDocument : Command
    {
        public CopyDocument(
            DocumentHandle handle,
            DocumentHandle copiedHandle)
        {
            Handle = handle;
            CopiedHandle = copiedHandle;
        }

        public DocumentHandle Handle { get; private set; }

        public DocumentHandle CopiedHandle { get; private set; }


    }
}
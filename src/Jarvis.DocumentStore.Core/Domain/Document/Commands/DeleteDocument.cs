using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class DeleteDocument : Command
    {
        public DeleteDocument(DocumentHandle handle)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }
}
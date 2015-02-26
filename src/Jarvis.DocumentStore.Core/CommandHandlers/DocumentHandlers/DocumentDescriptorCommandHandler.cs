using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public abstract class DocumentDescriptorCommandHandler<T> : RepositoryCommandHandler<DocumentDescriptor, T> where T : ICommand
    {
    
    }
}
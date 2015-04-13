using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
{
    public abstract class DocumentDescriptorCommandHandler<T> : RepositoryCommandHandler<DocumentDescriptor, T> where T : ICommand
    {
    
    }
}
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public abstract class DocumentCommandHandler<T> : RepositoryCommandHandler<Domain.Document.DocumentDescriptor, T> where T : ICommand
    {
    
    }
}
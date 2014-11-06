using CQRS.Kernel.Commands;
using CQRS.Shared.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public abstract class DocumentCommandHandler<T> : RepositoryCommandHandler<Domain.Document.Document, T> where T : ICommand
    {
    
    }
}
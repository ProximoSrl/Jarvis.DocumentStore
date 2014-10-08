using CQRS.Kernel.Commands;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;

namespace Jarvis.DocumentStore.Core.CommandHandlers
{
    public abstract class DocumentCommandHandler<T> : RepositoryCommandHandler<Document, T> where T : ICommand
    {
    
    }
}
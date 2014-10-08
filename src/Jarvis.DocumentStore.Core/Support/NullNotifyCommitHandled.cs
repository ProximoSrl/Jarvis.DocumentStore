using CQRS.Kernel.ProjectionEngine.Client;
using NEventStore;

namespace Jarvis.DocumentStore.Core.Support
{
    public class NullNotifyCommitHandled : INotifyCommitHandled
    {
        public void SetDispatched(ICommit commit)
        {
            
        }
    }
}
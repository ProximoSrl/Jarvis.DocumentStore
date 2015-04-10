using Castle.Core.Logging;
using Jarvis.Framework.Kernel.ProjectionEngine.Client;
using Jarvis.Framework.Shared.ReadModel;
using NEventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Support
{
    public class NotifyCommitHandled : INotifyCommitHandled
    {
        public ILogger Logger { get; set; }
        readonly IMessagesTracker _messageTracker;
        
        public NotifyCommitHandled(IMessagesTracker messageTracker)
        {
            _messageTracker = messageTracker;
        }

        public void SetDispatched(ICommit commit)
        {
            try
            {
                if (_messageTracker.Dispatched(commit.CommitId, DateTime.UtcNow))
                {
                    if (commit.Headers.ContainsKey("reply-to"))
                    {
                        //_bus.Publish(new CommitProjected(
                        //    commit.Headers["reply-to"].ToString(),
                        //    commit.CommitId,
                        //    "readmodel"
                        //    ));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Commit status update failed", ex);
            }
        }
    }
}

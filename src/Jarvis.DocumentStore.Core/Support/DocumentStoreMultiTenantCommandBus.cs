using Jarvis.Framework.Kernel.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.Framework.Shared.Commands;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using System.Security.Claims;

namespace Jarvis.DocumentStore.Core.Support
{
    public class DocumentStoreMultiTenantCommandBus : MultiTenantInProcessCommandBus
    {
        public DocumentStoreMultiTenantCommandBus(
            ITenantAccessor tenantAccessor, 
            IMessagesTracker messagesTracker) : base(tenantAccessor, messagesTracker)
        {
        }

        protected override string GetCurrentExecutingUser()
        {
             return ClaimsPrincipal.Current.Identity.Name;
        }

        protected override bool UserIsAllowedToSendCommand(ICommand command, string userName)
        {
            return true;
        }
    }
}

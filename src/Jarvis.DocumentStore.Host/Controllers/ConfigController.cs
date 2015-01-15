using CQRS.Shared.MultitenantSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class ConfigController : ApiController
    {
        public ITenantAccessor TenantAccessor { get; set; }

        [Route("config/tenants")]
        [HttpGet]
        public TenantId[] GetTenants()
        {
            var ids = TenantAccessor.Tenants.Select(x => x.Id).ToArray();

            return ids;
        }
    }
}

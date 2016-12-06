using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.Framework.Shared.MultitenantSupport;
using System.Diagnostics;
using System.Reflection;

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

        [HttpGet]
        [Route("config/getVersion")]
        public IHttpActionResult GetVersion()
        {
            var vi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var _version = vi.ProductVersion;
            if (_version.Length > 8)
                _version = _version.Substring(0, 8);
            var _release = vi.FileVersion;

            var info = new
            {
                Version = _version,
                Release = _release
            };

            return Ok(info);
        }
    }
}

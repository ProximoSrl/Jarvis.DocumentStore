using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Routing;
using Castle.Core.Logging;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.MultitenantSupport;

namespace Jarvis.DocumentStore.Host.Support
{
    public class TenantContextHandler : DelegatingHandler
    {
        private IExtendedLogger _logger;

        public TenantContextHandler(IExtendedLogger logger)
        {
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var routeData = request.GetConfiguration().Routes.GetRouteData(request);

            if (routeData != null)
            {
                var route = ((IHttpRouteData[])routeData.Values["MS_SubRoutes"]).First();

                if (route.Values.ContainsKey("tenantid"))
                {
                    string tenant = route.Values["tenantid"].ToString();
                    _logger.DebugFormat("Request {0} -> Tenant {1}", request.RequestUri, tenant);
                    TenantContext.Enter(new TenantId(tenant));
                }
            }

            var result = base.SendAsync(request, cancellationToken);

            return result;
        }
    }
}

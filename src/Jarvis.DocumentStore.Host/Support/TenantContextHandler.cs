using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Routing;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;

namespace Jarvis.DocumentStore.Host.Support
{
    public class TenantContextHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var route= ((IHttpRouteData[])request.GetConfiguration().Routes.GetRouteData(request).Values["MS_SubRoutes"]).First();

            if (route.Values.ContainsKey("tenantid"))
            {
                TenantContext.Enter(new TenantId(route.Values["tenantid"].ToString()));
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Support
{
    public static class RolesHelper
    {
        static RolesHelper()
        {
            var roles = ConfigurationManager.AppSettings["roles"];
            if (roles != null)
            {
                var roleArray = roles.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToArray();

                IsApiServer = roleArray.Contains("api");
                IsWorker = roleArray.Contains("worker");
                IsReadmodelBuilder = roleArray.Contains("projections");
            }
        }

        public static bool IsApiServer { get; private set; }
        public static bool IsWorker { get; private set; }
        public static bool IsReadmodelBuilder { get; private set; }
    }
}

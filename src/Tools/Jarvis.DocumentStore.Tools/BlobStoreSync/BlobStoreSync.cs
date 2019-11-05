using Castle.Core.Logging;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Shell.BlobStoreSync;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shell.BlobStoreSync
{
    public class BlobStoreSync
    {
        private readonly List<TenantConfiguration> _tenantsConfig = new List<TenantConfiguration>();
        private readonly ILogger _logger;

        public BlobStoreSync(ILogger logger)
        {
            var tenants = ConfigurationManager.AppSettings["tenants"].Split(',',';').Select(t => t.Trim()).ToList();
            foreach (string tenantId in tenants)
            {
                var config = new TenantConfiguration(tenantId, logger);
                _tenantsConfig.Add(config);
            }

            _logger = logger;
        }

        internal void SyncAllTenants(BlobStoreType source, BlobStoreType destination, Boolean startFromBeginning)
        {
            foreach (var tenant in _tenantsConfig)
            {
                var synchronyzer = new BlobStoreSynchronizer(tenant, _logger, startFromBeginning);
                synchronyzer.SetDirection(source, destination);

                _logger.Info($"Start synchronization of tenant {tenant.TenantId}");
                synchronyzer.PerformSync();
                _logger.Info($"Synchronization of tenant {tenant.TenantId} Completed. Press a key to continue!");
                Console.ReadKey();
            }
        }
    }
}

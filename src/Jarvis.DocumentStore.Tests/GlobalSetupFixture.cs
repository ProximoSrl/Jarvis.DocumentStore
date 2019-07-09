using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using Jarvis.Framework.Shared.MultitenantSupport;
using NUnit.Framework;
using System;
using System.Configuration;
using System.Reflection;

namespace Jarvis.DocumentStore.Tests
{
    [SetUpFixture]
    public class GlobalSetupFixture
    {
        [OneTimeSetUp]
        public void This_is_run_before_ANY_tests()
        {
            var overrideTestDb = Environment.GetEnvironmentVariable("TEST_MONGODB");
            if (!String.IsNullOrEmpty(overrideTestDb))
            {

                var overrideTestDbQueryString = Environment.GetEnvironmentVariable("TEST_MONGODB_QUERYSTRING");
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var connectionStringsSection = (ConnectionStringsSection)config.GetSection("connectionStrings");


                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "log", "ds-tests-logs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "ds.quartz", "ds-tests-quartz");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "ds.queue", "ds-tests-queues");

                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "ds.log.host", "ds-logs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "ds.quartz.host", "ds-quartz");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "ds.queue.host", "ds-queues");

                //<!-- Tenant 1-->
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "tests.originals", "ds-tests-ori-fs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "tests.artifacts", "ds-tests-art-fs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "tests.system", "ds-tests");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "tests.events", "ds-tests");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "tests.readmodel", "ds-tests");

                //<!-- Tenant DOCS -->
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "docs.originals", "ds-docs-ori-fs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "docs.artifacts", "ds-docs-art-fs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "docs.system", "ds-docs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "docs.events", "ds-docs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "docs.readmodel", "ds-docs");

                //<!-- Tenant DEMO -->
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "demo.originals", "ds-demo-ori-fs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "demo.artifacts", "ds-demo-art-fs");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "demo.system", "ds-demo");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "demo.events", "ds-demo");
                RewriteConnection(overrideTestDb, overrideTestDbQueryString, connectionStringsSection, "demo.readmodel", "ds-demo");

                config.Save();
                ConfigurationManager.RefreshSection("connectionStrings");
            }
            try
            {
                var mngr = new IdentityManager(new CounterService(MongoDbTestConnectionProvider.ReadModelDb));
                mngr.RegisterIdentitiesFromAssembly(typeof(DocumentDescriptorId).Assembly);
                mngr.RegisterIdentitiesFromAssembly(typeof(TenantId).Assembly);
                mngr.RegisterIdentitiesFromAssembly(typeof(QueuedJobId).Assembly);

                MongoFlatIdSerializerHelper.Initialize(mngr);
                //BsonSerializer.RegisterSerializationProvider(new EventStoreIdentitySerializationProvider());
                //BsonSerializer.RegisterSerializationProvider(new StringValueSerializationProvider());
                MongoFlatMapper.EnableFlatMapping(true);

            }
            catch (ReflectionTypeLoadException rle)
            {
                foreach (var ex in rle.LoaderExceptions)
                {
                    Console.WriteLine("Exception In typeloading: " + ex.Message);
                }
                Console.WriteLine("Exception in Global Setup: " + rle.ToString());
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in Global Setup: " + ex.ToString());
                throw;
            }
        }

    private static void RewriteConnection(
        string overrideTestDb,
        string overrideTestDbQueryString,
        ConnectionStringsSection connectionStringsSection,
        string connectionStringName,
        string databaseName)
    {
        connectionStringsSection.ConnectionStrings[connectionStringName].ConnectionString =
            overrideTestDb.TrimEnd('/') +
            "/" +
            databaseName.Trim('/') +
            overrideTestDbQueryString.Trim();
    }
}

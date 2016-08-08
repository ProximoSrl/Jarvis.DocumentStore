using System;
using System.Configuration;
using Jarvis.DocumentStore.Host.Support;
using MongoDB.Driver;
using Jarvis.Framework.Shared.Helpers;

namespace Jarvis.DocumentStore.Tests.Support
{
    public static class MongoDbTestConnectionProvider
    {
        static MongoDbTestConnectionProvider()
        {
            OriginalsDb = Connect("tests.originals");
            OriginalsDbLegacy = ConnectLegacy("tests.originals");
            ArtifactsDb = Connect("tests.artifacts");
            SystemDb = Connect("tests.system");
            EventsDb = Connect("tests.events");
            ReadModelDb = Connect("tests.readmodel");
            QueueDb = Connect("ds.queue");
        }

        static IMongoDatabase Connect(string connectionStringName)
        {
            var cstring = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (cstring == null)
            {
                throw new Exception(string.Format("Connection string {0} not found", connectionStringName));
            }
            
            var url = new MongoUrl(cstring.ConnectionString);

            var client = new MongoClient(url);
            return client.GetDatabase(url.DatabaseName);
        }

        static MongoDatabase ConnectLegacy(string connectionStringName)
        {
            var cstring = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (cstring == null)
            {
                throw new Exception(string.Format("Connection string {0} not found", connectionStringName));
            }

            var url = new MongoUrl(cstring.ConnectionString);

            var client = new MongoClient(url);
            return client.GetServer().GetDatabase(url.DatabaseName);
        }

        public static IMongoDatabase OriginalsDb { get; private set; }

        public static MongoDatabase OriginalsDbLegacy { get; private set; }

        public static IMongoDatabase ArtifactsDb { get; private set; }
        public static IMongoDatabase SystemDb { get; private set; }
        public static IMongoDatabase EventsDb { get; private set; }
        public static IMongoDatabase ReadModelDb { get; private set; }
        public static IMongoDatabase QueueDb { get; private set; }

        public static void DropTestsTenant()
        {
            OriginalsDb.Drop();
            ArtifactsDb.Drop();
            SystemDb.Drop();
            EventsDb.Drop();
            ReadModelDb.Drop();
            QueueDb.Drop();
        }

        public static void DropTenant(string tenant)
        {
            Connect(tenant + ".originals").Drop();
            Connect(tenant + ".artifacts").Drop();
            Connect(tenant + ".system").Drop();
            Connect(tenant + ".events").Drop();
            Connect(tenant + ".readmodel").Drop();
            Connect("ds.queue").Drop();
        }


        public static void DropAll()
        {
            DropTenant("docs");
            DropTenant("tests");
            DropTenant("demo");
            Connect("log").Drop();
            Connect("ds.quartz").Drop();
            Connect("ds.queue").Drop();
            Connect("ds.queue.host").Drop();
            Connect("ds.quartz.host").Drop();
            Connect("ds.log.host").Drop();
        }
    }
}

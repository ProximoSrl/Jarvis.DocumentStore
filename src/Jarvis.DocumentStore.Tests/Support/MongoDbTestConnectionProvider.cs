using System;
using System.Configuration;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Host.Support;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Tests.Support
{
    public static class MongoDbTestConnectionProvider
    {
        static MongoDbTestConnectionProvider()
        {
            OriginalsDb = Connect("tests.originals");
            ArtifactsDb = Connect("tests.artifacts");
            SystemDb = Connect("tests.system");
            EventsDb = Connect("tests.events");
            ReadModelDb = Connect("tests.readmodel");
        }

        static MongoDatabase Connect(string connectionStringName)
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

        public static MongoDatabase OriginalsDb { get; private set; }
        public static MongoDatabase ArtifactsDb { get; private set; }
        public static MongoDatabase SystemDb { get; private set; }
        public static MongoDatabase EventsDb { get; private set; }
        public static MongoDatabase ReadModelDb { get; private set; }

        public static void DropTestsTenant()
        {
            OriginalsDb.Drop();
            ArtifactsDb.Drop();
            SystemDb.Drop();
            EventsDb.Drop();
            ReadModelDb.Drop();
        }

        public static void DropTenant(string tenant)
        {
            Connect(tenant + ".originals").Drop();
            Connect(tenant + ".artifacts").Drop();
            Connect(tenant + ".system").Drop();
            Connect(tenant + ".events").Drop();
            Connect(tenant + ".readmodel").Drop();
        }


        public static void DropAll()
        {
            DropTenant("docs");
            DropTenant("tests");
            DropTenant("demo");
            Connect("log").Drop();
            Connect("ds.quartz").Drop();
            Connect("ds.queues").Drop();
            Connect("ds.queues.host").Drop();
            Connect("ds.quartz.host").Drop();
            Connect("ds.log.host").Drop();
        }
    }
}

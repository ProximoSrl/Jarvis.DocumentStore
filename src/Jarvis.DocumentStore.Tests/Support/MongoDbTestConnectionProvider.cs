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
            FileStoreDb = Connect("tests.filestore");
            SystemDb = Connect("tests.system");
            EventsDb = Connect("tests.events");
            ReadModelDb = Connect("tests.readmodel");
        }

        static MongoDatabase Connect(string connectionStringName)
        {
            var url = new MongoUrl(
                ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString
            );

            var client = new MongoClient(url);
            return client.GetServer().GetDatabase(url.DatabaseName);
        }

        public static MongoDatabase FileStoreDb { get; private set; }
        public static MongoDatabase SystemDb { get; private set; }
        public static MongoDatabase EventsDb { get; private set; }
        public static MongoDatabase ReadModelDb { get; private set; }

        public static void DropTenant1()
        {
            FileStoreDb.Drop();
            SystemDb.Drop();
            EventsDb.Drop();
            ReadModelDb.Drop();
        }

        public static void DropTenant(string tenant)
        {
            Connect(tenant + ".filestore").Drop();
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
            
            Connect("ds.quartz.host").Drop();
            Connect("ds.log.host").Drop();
        }
    }
}

using System.Configuration;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Tests.Support
{
    public static class MongoDbTestConnectionProvider
    {
        static MongoDbTestConnectionProvider()
        {
            var url = new MongoUrl(
                ConfigurationManager.ConnectionStrings["tests"].ConnectionString
            );

            var client = new MongoClient(url);
            TestDb = client.GetServer().GetDatabase(url.DatabaseName);
        }

        public static MongoDatabase TestDb { get; private set; }
    }
}

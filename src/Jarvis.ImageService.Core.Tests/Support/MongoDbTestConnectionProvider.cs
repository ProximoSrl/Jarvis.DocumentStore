using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Jarvis.ImageService.Core.Tests.Support
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

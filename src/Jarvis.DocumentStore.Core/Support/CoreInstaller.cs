using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CoreInstaller : IWindsorInstaller
    {
        readonly string _connectionString;

        public CoreInstaller(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<IFileStore>()
                    .ImplementedBy<GridFSFileStore>(),
                Component
                    .For<IFileService>()
                    .ImplementedBy<MongoDbFileService>(),
                Component
                    .For<ConfigService>(),
                Component
                    .For<LibreOfficeConversion>()
                    .LifestyleTransient(),
                Component
                    .For<MongoDatabase>()
                    .Instance(GetDatabase())
            );
        }

        MongoDatabase GetDatabase()
        {
            var mongoUrl = new MongoUrl(_connectionString);
            var client = new MongoClient(_connectionString);
            var db = client.GetServer().GetDatabase(mongoUrl.DatabaseName);
            return db;
        }
    }
}

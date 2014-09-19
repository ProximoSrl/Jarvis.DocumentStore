using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using MongoDB.Driver;

namespace Jarvis.ImageService.Core.Support
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
                    .For<ICounterService>()
                    .ImplementedBy<CounterService>(),
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

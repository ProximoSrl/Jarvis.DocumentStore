using System.Configuration;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Factories;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.Stats;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CoreInstaller : IWindsorInstaller
    {
        readonly TenantManager _manager;

        public CoreInstaller(TenantManager manager)
        {
            _manager = manager;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component
                    .For(typeof(IFactory<>))
                    .AsFactory(),
                Component
                    .For<IFileStore>()
                    .ImplementedBy<GridFSFileStore>()
                    .LifeStyle.Transient,
                Component
                    .For<GridFsFileStoreStats>()
                    .LifeStyle.Transient,
                Component
                    .For<ConfigService>(),
                Component
                    .For<ILibreOfficeConversion>()
                    .ImplementedBy<LibreOfficeUnoConversion>()
                    .LifeStyle.Transient,
                Component
                    .For<IDocumentMapper>()
                    .ImplementedBy<DocumentMapper>()
                    .LifeStyle.Transient
            );
        }

        MongoDatabase GetDatabase(string cstring)
        {
            var mongoUrl = new MongoUrl(cstring);
            var client = new MongoClient(cstring);
            var db = client.GetServer().GetDatabase(mongoUrl.DatabaseName);
            return db;
        }
    }
}

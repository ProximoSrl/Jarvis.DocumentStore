using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CoreInstaller : IWindsorInstaller
    {
        readonly string _fileStore;
        readonly string _sysDb;

        public CoreInstaller(string fileStore, string sysDb)
        {
            _fileStore = fileStore;
            _sysDb = sysDb;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var sysdb = GetDatabase(_sysDb);

            container.Register(
                Component
                    .For<IFileStore>()
                    .ImplementedBy<GridFSFileStore>(),
                Component
                    .For<ConfigService>(),
                Component
                    .For<ILibreOfficeConversion>()
                    .ImplementedBy<LibreOfficeUnoConversion>(),
                Component
                    .For<IDocumentMapper>()
                    .ImplementedBy<DocumentMapper>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(sysdb)),
                Component
                    .For<IFileAliasMapper>()
                    .ImplementedBy<FileAliasMapper>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(sysdb)),
                Component
                    .For<MongoDatabase>()
                    .Instance(GetDatabase(_fileStore))
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

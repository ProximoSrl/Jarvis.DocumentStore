using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class DocumentProjectionTests
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private ICommandBus _bus;
        IFileStore _filestore;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new DocumentStoreTestConfiguration { UseOnlyInMemoryBus = true };
            MongoDbTestConnectionProvider.DropTenant1();
            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _documentStoreService.Start(config);

            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _bus = tenant.Container.Resolve<ICommandBus>();
            _filestore = tenant.Container.Resolve<IFileStore>();
            Assert.IsTrue(_bus is IInProcessCommandBus);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        FileId Upload(string id, string pathToFile)
        {
            var fileId = new FileId(id);
            _filestore.Upload(fileId, pathToFile );
            return fileId;
        }

        [Test]
        public void run()
        {
            _bus.Send(new CreateDocument(
                new DocumentId(1),
                Upload("file_1", TestConfig.PathToDocumentPdf),
                new DocumentHandle("handle_1"),
                new FileNameWithExtension("file_1.pdf"), null)
            );

            _bus.Send(new CreateDocument(
                new DocumentId(2),
                Upload("file_2", TestConfig.PathToDocumentPng),
                new DocumentHandle("handle_2"),
                new FileNameWithExtension("file_2.png"), null)
            );

            _bus.Send(new CreateDocument(
                new DocumentId(3),
                Upload("file_3", TestConfig.PathToOpenDocumentSpreadsheet),
                new DocumentHandle("handle_1"),
                new FileNameWithExtension("file_3.xlsx"), null)
            );

            Thread.Sleep(5000);
        }
    }
}

using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Controllers;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using NSubstitute;
using NUnit.Framework;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Tests.Support;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    public abstract class AbstractFileControllerTests
    {
        protected DocumentsController Controller;
        protected IBlobStore BlobStore;
        protected IIdentityGenerator IdentityGenerator;
        protected ICounterService CounterService;
        protected IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId> DocumentReader;
        protected IMongoDbReader<DocumentDeletedReadModel, String> DocumentDeletedReader;
        protected TenantId _tenantId = new TenantId("docs");
        IDocumentWriter _handleWriter;
        protected IQueueManager QueueDispatcher;
        [SetUp]
        public void SetUp()
        {
            BlobStore = Substitute.For<IBlobStore>();
            IdentityGenerator = Substitute.For<IIdentityGenerator>();
            _handleWriter = Substitute.For<IDocumentWriter>();
            DocumentReader = Substitute.For<IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();
            DocumentDeletedReader = Substitute.For<IMongoDbReader<DocumentDeletedReadModel, String>>();

            QueueDispatcher = Substitute.For<IQueueManager>();
            CounterService = Substitute.For<ICounterService>();
            var bus = Substitute.For<IInProcessCommandBus>();
            var configuration =  new DocumentStoreTestConfiguration();
            Controller = new DocumentsController(
                BlobStore,
               configuration, 
                IdentityGenerator, 
                DocumentReader,
                DocumentDeletedReader,
                bus, 
                _handleWriter,
                QueueDispatcher,
                CounterService,
                null)
            {
                Request = new HttpRequestMessage
                {
                    RequestUri = new Uri("http://localhost/api/products")
                },
                Logger = new ConsoleLogger(),
                Configuration = new HttpConfiguration(),
                RequestContext =
                {
                    RouteData = new HttpRouteData(
                        route: new HttpRoute(),
                        values: new HttpRouteValueDictionary {{"controller", "file"}})
                }
            };

            //  Controller.Configuration.MapHttpAttributeRoutes();

            Controller.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
        }

        protected void SetupDocumentModel(DocumentDescriptorReadModel doc)
        {
            this.DocumentReader.FindOneById(doc.Id).Returns(info => doc);
        }

        protected void SetupDocumentHandle(DocumentHandleInfo handleInfo, DocumentDescriptorId documentId)
        {
            _handleWriter
                .FindOneById(handleInfo.Handle)
                .Returns(info => new DocumentReadModel(handleInfo.Handle, documentId, handleInfo.FileName));
        }
    }
}
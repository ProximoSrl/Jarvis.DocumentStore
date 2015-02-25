using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Controllers;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using NSubstitute;
using NUnit.Framework;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    public abstract class AbstractFileControllerTests
    {
        protected DocumentsController Controller;
        protected IBlobStore BlobStore;
        protected IIdentityGenerator IdentityGenerator;
        protected ICounterService CounterService;
        protected IReader<DocumentDescriptorReadModel, DocumentDescriptorId> DocumentReader;
        protected TenantId _tenantId = new TenantId("docs");
        IHandleWriter _handleWriter;
        protected IQueueDispatcher QueueDispatcher;
        [SetUp]
        public void SetUp()
        {
            BlobStore = Substitute.For<IBlobStore>();
            IdentityGenerator = Substitute.For<IIdentityGenerator>();
            _handleWriter = Substitute.For<IHandleWriter>();
            DocumentReader = Substitute.For<IReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();
            QueueDispatcher= Substitute.For<IQueueDispatcher>();
            CounterService = Substitute.For<ICounterService>();
            var bus = Substitute.For<IInProcessCommandBus>();

            Controller = new DocumentsController(
                BlobStore, 
                new ConfigService(), 
                IdentityGenerator, 
                DocumentReader, 
                bus, 
                _handleWriter,
                QueueDispatcher,
                CounterService)
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
                .Returns(info => new HandleReadModel(handleInfo.Handle, documentId, handleInfo.FileName));
        }
    }
}
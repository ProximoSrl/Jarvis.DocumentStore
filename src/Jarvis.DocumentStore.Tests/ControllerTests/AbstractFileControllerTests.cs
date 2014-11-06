using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Castle.Core.Logging;
using CQRS.Kernel.Commands;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Controllers;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    public abstract class AbstractFileControllerTests
    {
        protected DocumentsController Controller;
        protected IBlobStore BlobStore;
        protected IIdentityGenerator IdentityGenerator;
        protected IReader<ExHandleToDocument, DocumentHandle> HandleToDocumentReader;
        protected IReader<DocumentReadModel, DocumentId> DocumentReader;
        protected TenantId _tenantId = new TenantId("docs");

        [SetUp]
        public void SetUp()
        {
            BlobStore = Substitute.For<IBlobStore>();
            IdentityGenerator = Substitute.For<IIdentityGenerator>();
            HandleToDocumentReader = Substitute.For<IReader<ExHandleToDocument, DocumentHandle>>();
            DocumentReader = Substitute.For<IReader<DocumentReadModel, DocumentId>>();
            var bus = Substitute.For<IInProcessCommandBus>();

            Controller = new DocumentsController(BlobStore, new ConfigService(), IdentityGenerator, HandleToDocumentReader, DocumentReader, bus)
            {
                Request = new HttpRequestMessage
                {
                    RequestUri = new Uri("http://localhost/api/products")
                },
                Logger = new ConsoleLogger(),
                Configuration = new HttpConfiguration()
            };

            //  Controller.Configuration.MapHttpAttributeRoutes();

            Controller.RequestContext.RouteData = new HttpRouteData(
                route: new HttpRoute(),
                values: new HttpRouteValueDictionary { { "controller", "file" } });
            
            Controller.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
        }

        protected void SetupDocumentModel(DocumentReadModel doc)
        {
            this.DocumentReader.FindOneById(doc.Id).Returns(info => doc);
        }

        protected void SetupDocumentHandle(DocumentHandleInfo handleInfo, DocumentId documentId)
        {
            HandleToDocumentReader
                .FindOneById(handleInfo.Handle)
                .Returns(info => new ExHandleToDocument(handleInfo, documentId));
        }
    }
}
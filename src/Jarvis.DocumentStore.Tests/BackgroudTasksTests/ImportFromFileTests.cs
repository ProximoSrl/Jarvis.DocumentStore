using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.BackgroundTasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.Framework.Shared.Commands;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.BackgroudTasksTests
{
    [TestFixture]
    public class ImportFromFileTests
    {
        private ImportFormatFromFileQueue _queue;
        private string _pathToTask;
        private readonly TenantId _testTenant = new TenantId("tests");
        private IBlobStore _blobstore;
        private readonly DocumentFormat _originalFormat = new DocumentFormat("original");
        private readonly DocumentHandle _documentHandle = new DocumentHandle("word");
        private readonly Uri _fileUri = new Uri(TestConfig.PathToWordDocument);
        private ICommandBus _commandBus;
        private BlobId _blobId;

        [SetUp]
        public void SetUp()
        {
            _blobId = new BlobId(_originalFormat, 1);
            _pathToTask = Path.Combine(TestConfig.QueueFolder, "File_1.dsimport");

            var accessor = Substitute.For<ITenantAccessor>();
            var tenant = Substitute.For<ITenant>();
            var container = Substitute.For<IWindsorContainer>();
            _commandBus = Substitute.For<ICommandBus>();
            var identityGenerator = Substitute.For<IIdentityGenerator>();

            _blobstore = Substitute.For<IBlobStore>();
            _blobstore.Upload(Arg.Is(_originalFormat), Arg.Any<string>()).Returns(_blobId);

            accessor.GetTenant(_testTenant).Returns(tenant);
            accessor.Current.Returns(tenant);
            tenant.Container.Returns(container);

            container.Resolve<IBlobStore>().Returns(_blobstore);
            container.Resolve<IIdentityGenerator>().Returns(identityGenerator);

            _queue = new ImportFormatFromFileQueue(new[] { TestConfig.QueueFolder }, accessor, _commandBus)
            {
                Logger = new ConsoleLogger()
            };
        }

        [Test]
        public void should_load_task()
        {
            var descriptor = _queue.LoadTask(_pathToTask);

            Assert.NotNull(descriptor);
            Assert.AreEqual(_fileUri, descriptor.Uri);
            Assert.AreEqual(_originalFormat, descriptor.Format);
            Assert.AreEqual(_documentHandle, descriptor.Handle);
            Assert.AreEqual(_testTenant, descriptor.Tenant);
            Assert.IsFalse(descriptor.DeleteAfterImport);
            Assert.NotNull(descriptor.CustomData);
            Assert.AreEqual("2050-01-01", descriptor.CustomData["expire-on"]);
        }

        [Test]
        public void poll()
        {
            InitializeDocumentDescriptor command = null;
            _commandBus.When(c => c.Send(Arg.Any<InitializeDocumentDescriptor>(), Arg.Any<string>()))
               .Do(callInfo => command = (InitializeDocumentDescriptor)callInfo.Args()[0]);

            _queue.PollFileSystem();

            // asserts
            _blobstore.Received().Upload(Arg.Is(_originalFormat),Arg.Any<string>());
            
            Assert.NotNull(command);
            Assert.AreEqual(_blobId, command.BlobId);
            Assert.NotNull(command.HandleInfo);
            Assert.AreEqual(_documentHandle, command.HandleInfo.Handle);
        }
    }
}
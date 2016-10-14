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
using MongoDB.Driver;
using Jarvis.DocumentStore.Tests.Support;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

using Jarvis.Framework.Shared.Helpers;

namespace Jarvis.DocumentStore.Tests.BackgroudTasksTests
{
    [TestFixture]
    public class ImportFromFileTests
    {
        private ImportFormatFromFileQueue _queue;
        private string _pathToTask;
        private string _fileToImport;
        private readonly TenantId _testTenant = new TenantId("tests");
        private IBlobStore _blobstore;
        private readonly DocumentFormat _originalFormat = new DocumentFormat("original");
        private readonly DocumentHandle _documentHandle = new DocumentHandle("word");
        private readonly Uri _fileUri = new Uri(Path.Combine(TestConfig.QueueFolder, "A word document.docx"));
        private ICommandBus _commandBus;
        private BlobId _blobId;

        [SetUp]
        public void SetUp()
        {
            _blobId = new BlobId(_originalFormat, 1);
            _pathToTask = Path.Combine(TestConfig.QueueFolder, "File_1.dsimport");
            _fileToImport = Path.Combine(TestConfig.QueueFolder, "A Word Document.docx");
            ClearQueueTempFolder();
            Directory.CreateDirectory(TestConfig.QueueFolder);
            File.Copy(Path.Combine(TestConfig.DocumentsFolder, "Queue\\File_1.dsimport"), _pathToTask);
            File.Copy(TestConfig.PathToWordDocument, _fileToImport);
            var accessor = Substitute.For<ITenantAccessor>();
            var tenant = Substitute.For<ITenant>();
            tenant.Id.Returns(new TenantId("tests"));
            var container = Substitute.For<IWindsorContainer>();
            _commandBus = Substitute.For<ICommandBus>();
            var identityGenerator = Substitute.For<IIdentityGenerator>();

            _blobstore = Substitute.For<IBlobStore>();
            _blobstore.Upload(Arg.Is(_originalFormat), Arg.Any<string>()).Returns(_blobId);
            _blobstore.Upload(Arg.Is(_originalFormat), Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>()).Returns(_blobId);

            accessor.GetTenant(_testTenant).Returns(tenant);
            accessor.Current.Returns(tenant);
            tenant.Container.Returns(container);

            container.Resolve<IBlobStore>().Returns(_blobstore);
            container.Resolve<IIdentityGenerator>().Returns(identityGenerator);
            container.Resolve<IMongoDatabase>().Returns(MongoDbTestConnectionProvider.ReadModelDb);
            var collection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<ImportFailure>("sys.importFailures");
            collection.Drop();
            DocumentStoreTestConfiguration config = new DocumentStoreTestConfiguration(tenantId : "tests");
            config.SetFolderToMonitor(TestConfig.QueueFolder);
            var sysDb = config.TenantSettings.Single(t => t.TenantId == "tests").Get<IMongoDatabase>("system.db");
            sysDb.Drop();
            _queue = new ImportFormatFromFileQueue(config, accessor, _commandBus)
            {
                Logger = new ConsoleLogger()
            };

            _queue.DeleteTaskFileAfterImport = false;
        }

        private static void ClearQueueTempFolder()
        {
            if (Directory.Exists(TestConfig.QueueFolder))
            {
                foreach (var file in Directory.GetFiles(TestConfig.QueueFolder))
                {
                    File.SetAttributes(file, FileAttributes.Archive);
                }
                Directory.Delete(TestConfig.QueueFolder, true);
            }
        }

        [TearDown]
        public void TearDown()
        {
            ClearQueueTempFolder();
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
        public void correctly_delete_file_after_import()
        {
            _queue.DeleteTaskFileAfterImport = true;
            var descriptor = _queue.LoadTask(_pathToTask);
            _queue.UploadFile(_pathToTask, descriptor);
            Assert.That(File.Exists(_pathToTask), Is.False);
        }

        [Test]
        public void what_happens_if_task_file_cannot_be_deleted()
        {
            _queue.DeleteTaskFileAfterImport = true;
            var attribute = File.GetAttributes(_pathToTask);
            File.SetAttributes(_pathToTask, attribute | FileAttributes.ReadOnly);

            _queue.PollFileSystem();
            Assert.That(File.Exists(_pathToTask), Is.True); //file cannot be deleted
            _commandBus.Received(1).Send(Arg.Any<ICommand>(), "import-from-file");
            _queue.PollFileSystem();
            _commandBus.Received(1).Send(Arg.Any<ICommand>(), "import-from-file");
        }

        [Test]
        public void task_file_cannot_be_deleted_but_then_modified_to_retry()
        {
            _queue.DeleteTaskFileAfterImport = true;
            var attribute = File.GetAttributes(_pathToTask);
            File.SetAttributes(_pathToTask, attribute | FileAttributes.ReadOnly);

            _queue.PollFileSystem();
            Assert.That(File.Exists(_pathToTask), Is.True); //file cannot be deleted
            _commandBus.Received(1).Send(Arg.Any<ICommand>(), "import-from-file");
            _queue.PollFileSystem();
            _commandBus.Received(1).Send(Arg.Any<ICommand>(), "import-from-file");
            attribute = File.GetAttributes(_pathToTask);
            File.SetAttributes(_pathToTask, attribute & ~FileAttributes.ReadOnly);

            File.SetLastWriteTime(_pathToTask, DateTime.UtcNow);
            _queue.PollFileSystem();
            _commandBus.Received(2).Send(Arg.Any<ICommand>(), "import-from-file");
        }

        [Test]
        public void poll()
        {
            InitializeDocumentDescriptor command = null;
            _commandBus.When(c => c.Send(Arg.Any<InitializeDocumentDescriptor>(), Arg.Any<string>()))
               .Do(callInfo => command = (InitializeDocumentDescriptor)callInfo.Args()[0]);

            _queue.PollFileSystem();
               
            // asserts
            _blobstore.Received().Upload(Arg.Is(_originalFormat), Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>());
            
            Assert.NotNull(command);
            Assert.AreEqual(_blobId, command.BlobId);
            Assert.NotNull(command.HandleInfo);
            Assert.AreEqual(_documentHandle, command.HandleInfo.Handle);
        }
    }
}
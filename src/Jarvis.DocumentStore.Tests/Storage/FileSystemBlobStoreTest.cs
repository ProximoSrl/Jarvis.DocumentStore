using Jarvis.DocumentStore.Core.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Storage.FileSystem;

namespace Jarvis.DocumentStore.Tests.Storage
{
    [TestFixture]
    public class FileSystemBlobStoreTest : BlobStoreTestBase
    {
        FileSystemBlobStore _sut;
        private String _tempLocalDirectory;
        DirectoryManager _directoryManager;

        [SetUp]
        public void SetUp()
        {
            _tempLocalDirectory = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(_tempLocalDirectory);
            _directoryManager = new DirectoryManager(_tempLocalDirectory);

            _sut = new FileSystemBlobStore(MongoDbTestConnectionProvider.OriginalsDb,
                "originals",
                _tempLocalDirectory,
                new CounterService(MongoDbTestConnectionProvider.SystemDb))
            {
                Logger = new ConsoleLogger()
            };
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_tempLocalDirectory, true);
        }

        [Test]
        public void Verify_delete_stream_delete_original_file_not_only_descriptor()
        {
            String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "test1.txt");
            var id = _sut.Upload(DocumentFormats.Original, tempFileName);
            Assert.That(File.Exists(_directoryManager.GetFileNameFromBlobId(id)));
            Assert.That(id, Is.Not.Null);
            _sut.Delete(id);
            Assert.That(File.Exists(_directoryManager.GetFileNameFromBlobId(id)), Is.False);
        }
    }
}

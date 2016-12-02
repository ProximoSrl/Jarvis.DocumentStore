using Castle.Core.Logging;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.GridFs;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.Storage
{
    [TestFixture("filesystem")]
    [TestFixture("gridfs")]
    public class GenericBlobStoreTests : BlobStoreTestBase
    {
        IBlobStore _sut;
        private readonly String _blobStoreToTest;
        private String _tempLocalDirectory;

        public GenericBlobStoreTests(String blobStoreToTest)
        {
            _blobStoreToTest = blobStoreToTest;
        }

        [SetUp]
        public void SetUp()
        {
            _tempLocalDirectory = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(_tempLocalDirectory);

            if (_blobStoreToTest == "filesystem")
            {
                _sut = new FileSystemBlobStore(MongoDbTestConnectionProvider.OriginalsDb,
                    "originals",
                    _tempLocalDirectory,
                    new CounterService(MongoDbTestConnectionProvider.SystemDb))
                {
                    Logger = new ConsoleLogger()
                };
            }
            else if (_blobStoreToTest == "gridfs")
            {
                MongoDbTestConnectionProvider.DropTestsTenant();

                _sut = new GridFsBlobStore
                (
                    MongoDbTestConnectionProvider.OriginalsDbLegacy,
                    new CounterService(MongoDbTestConnectionProvider.SystemDb)
                )
                {
                    Logger = new ConsoleLogger()
                };
            }
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_tempLocalDirectory, true);
        }

        [Test]
        public void Verify_basic_save_and_reload_stream()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "test1.txt");
            var id = _sut.Upload(DocumentFormats.Original, tempFileName);
            Assert.That(id, Is.Not.Null);

            var download = _sut.Download(id, _tempLocalDirectory);
            Assert.That(Path.GetFileName(download), Is.EqualTo("test1.txt"));
            Assert.That(File.ReadAllText(download), Is.EqualTo(content));
        }

        [Test]
        public void Verify_basic_create_new_and_get_descriptor()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id;
            using (var writer = _sut.CreateNew(DocumentFormats.Original, new Core.Model.FileNameWithExtension(tempFileName)))
            using (var fileStream = new FileStream(tempFileName, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(writer.WriteStream);
                id = writer.BlobId;
            }
            Assert.That(id, Is.Not.Null);

            var descriptor = _sut.GetDescriptor(id);
            Assert.That(descriptor.BlobId, Is.EqualTo(id));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));
        }

        /// <summary>
        /// This is a standard problem, because of how the itnerface of documenstore are created, the
        /// IBlobWriter is <see cref="IDisposable" /> but is not correctly disposed by the api. 
        /// 
        /// </summary>
        [Test]
        public void Verify_writing_on_a_write_stream_with_only_dispose_of_the_stream_set_descriptor()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id;
            var writer = _sut.CreateNew(DocumentFormats.Original, new Core.Model.FileNameWithExtension(tempFileName));
            using (writer.WriteStream)
            {
                using (var fileStream = new FileStream(tempFileName, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(writer.WriteStream);
                    id = writer.BlobId;
                }
            }
            Assert.That(id, Is.Not.Null);

            var descriptor = _sut.GetDescriptor(id);
            Assert.That(descriptor.BlobId, Is.EqualTo(id));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));
        }

        [Test]
        public void Verify_writing_on_a_write_stream_async_with_only_dispose_of_the_stream_set_descriptor()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id;
            var writer = _sut.CreateNew(DocumentFormats.Original, new Core.Model.FileNameWithExtension(tempFileName));
            using (writer.WriteStream)
            {
                id = writer.BlobId;
                var allBytes = File.ReadAllBytes(tempFileName);
                writer.WriteStream.WriteAsync(allBytes, 0, allBytes.Length).Wait();
            }
            Assert.That(id, Is.Not.Null);

            var descriptor = _sut.GetDescriptor(id);
            Assert.That(descriptor.BlobId, Is.EqualTo(id));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));
        }

        [Test]
        public void Verify_writing_on_a_write_stream_one_byte_at_a_time_with_only_dispose_of_the_stream_set_descriptor()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id;
            var writer = _sut.CreateNew(DocumentFormats.Original, new Core.Model.FileNameWithExtension(tempFileName));
            using (writer.WriteStream)
            {
                id = writer.BlobId;
                var allBytes = File.ReadAllBytes(tempFileName);
                foreach (var b in allBytes)
                {
                    writer.WriteStream.WriteByte(b);
                }
            }
            Assert.That(id, Is.Not.Null);

            var descriptor = _sut.GetDescriptor(id);
            Assert.That(descriptor.BlobId, Is.EqualTo(id));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));
        }

        [Test]
        public void Verify_descriptor_can_access_the_stream()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id;
            using (var writer = _sut.CreateNew(DocumentFormats.Original, new Core.Model.FileNameWithExtension(tempFileName)))
            using (var fileStream = new FileStream(tempFileName, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(writer.WriteStream);
                id = writer.BlobId;
            }
            Assert.That(id, Is.Not.Null);

            var descriptor = _sut.GetDescriptor(id);
            using (var stream = descriptor.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                String value = reader.ReadToEnd();
                Assert.That(value, Is.EqualTo(content));
            }
        }

        [Test]
        public void Verify_upload_and_get_descriptor()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            var id = _sut.Upload(DocumentFormats.Original, tempFileName);
            Assert.That(id, Is.Not.Null);

            var descriptor = _sut.GetDescriptor(id);
            Assert.That(descriptor.BlobId, Is.EqualTo(id));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));
        }

        [Test]
        public void Verify_upload_with_stream_and_get_descriptor()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            using (var fileStream = new FileStream(tempFileName, FileMode.Open, FileAccess.Read))
            {
                var id = _sut.Upload(DocumentFormats.Original, new FileNameWithExtension(tempFileName), fileStream);
                Assert.That(id, Is.Not.Null);

                var descriptor = _sut.GetDescriptor(id);
                Assert.That(descriptor.BlobId, Is.EqualTo(id));
                Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
                Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
                Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
                Assert.That(descriptor.Length, Is.EqualTo(31));
            }
        }

        [Test]
        public void Verify_different_file_names_generates_different_blob_id()
        {
            const String content = "this is the content of the file";
            String tempFileName1 = GenerateTempTextFile(content, "test1.txt");
            String tempFileName2 = GenerateTempTextFile(content, "test2.txt");
            var id1 = _sut.Upload(DocumentFormats.Original, tempFileName1);
            var id2 = _sut.Upload(DocumentFormats.Original, tempFileName2);

            Assert.That(id1, Is.Not.EqualTo(id2), "If content is equal but we have different file the blobId should be different");
        }

        [Test]
        public void Verify_delete_stream()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "test1.txt");
            var id = _sut.Upload(DocumentFormats.Original, tempFileName);
            Assert.That(id, Is.Not.Null);
            _sut.Delete(id);

            Assert.Throws<Exception>(() =>_sut.GetDescriptor(id));
        }
    }
}

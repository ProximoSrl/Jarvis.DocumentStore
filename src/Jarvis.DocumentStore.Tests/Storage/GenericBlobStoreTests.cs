using Castle.Core.Logging;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.FileSystem;
using Jarvis.DocumentStore.Core.Storage.GridFs;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using NUnit.Framework;
using System;
using System.IO;

namespace Jarvis.DocumentStore.Tests.Storage
{
    [TestFixture("filesystem")]
    [TestFixture("gridfs")]
    public class GenericBlobStoreTests : BlobStoreTestBase
    {
        private GridFsBlobStore _gridfsStore;
        private IBlobStore _sut;
        private IBlobStoreAdvanced _sutAdvanced;
        private DirectoryManager _directoryManager;
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

            _gridfsStore = new GridFsBlobStore
            (
                MongoDbTestConnectionProvider.OriginalsDbLegacy,
                new CounterService(MongoDbTestConnectionProvider.SystemDb)
            )
            {
                Logger = new ConsoleLogger()
            };

            if (_blobStoreToTest == "filesystem")
            {
                var fsStore = new FileSystemBlobStore(
                    MongoDbTestConnectionProvider.OriginalsDb,
                    FileSystemBlobStore.OriginalDescriptorStorageCollectionName,
                    _tempLocalDirectory,
                    new CounterService(MongoDbTestConnectionProvider.SystemDb),
                    "",
                    "")
                {
                    Logger = new ConsoleLogger()
                };
                _sut = fsStore;
                _sutAdvanced = fsStore as IBlobStoreAdvanced;
                _directoryManager = new Core.Storage.FileSystem.DirectoryManager(_tempLocalDirectory, 3);
            }
            else if (_blobStoreToTest == "gridfs")
            {
                MongoDbTestConnectionProvider.DropTestsTenant();

                _sut = _gridfsStore;
                _sutAdvanced = _gridfsStore as IBlobStoreAdvanced;
            }
        }

        [TearDown]
        public void TearDown()
        {
            Pri.LongPath.Directory.Delete(_tempLocalDirectory, true);
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
        public void Verify_basic_save_and_reload_stream_with_problematic_too_long_file_name()
        {
            if (_blobStoreToTest == "filesystem")
            {
                const String content = "this is the content of the file";
                string fileName = "xxxxxx x xxx xxxxx xxxxxxxxxxx xxxxx xxx xxxxxxxx xxxxxxxxx xxxxx xx xxxxxxxxxxx xxxxx . xxxxx xxxxxxx xx xxxx xxxxxxx xxxxx xxxxx, xx xxxxxxxxxx xxxx xxxxxxr xxxxxx xxxx xx xxxxxxx 2 xxxxxxxxs xxx xxxx xxxxx xxxxxxxxxx xxx xxx xxxx xxxxxxxxxxxx xxxxx..eml";
                String tempFileName = GenerateTempTextFile(content, "shortfile.txt");
                BlobId blobId = new BlobId(new Core.Domain.DocumentDescriptor.DocumentFormat("original"), 12345456);

                using (var fs = new FileStream(tempFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //We can create in gridfs a descriptor with a filename that is not admittible in file system.
                    var savedBlobId = _gridfsStore.Upload(
                        new Core.Domain.DocumentDescriptor.DocumentFormat("original"),
                        new FileNameWithExtension(fileName),
                        fs);

                    var descriptor = _gridfsStore.GetDescriptor(savedBlobId);

                    //Try to store the file with that too long file name.
                    _sutAdvanced.RawStore(blobId, descriptor);

                    var download = _sut.Download(blobId, _tempLocalDirectory);
                    //name of the file is mangled, so we can retrieve the content downloading to a temp file.
                    Assert.That(Path.GetFileName(download), Is.EqualTo("xxxxxx x xxx xxxxx xxxxxxxxxxx xxxxx xxx xxxxxxxx xxxxxxxxx xxxxx xx xxxxxxxxxxx xxxxx . xxxxx xxxxxxx xx xxxx xxxxxxx xxxxx xxxxx, xx xxxxxxxxxx xxxx xxxxxxr xxxxxx xxxx xx xxxxxxx 2 xxxxxxxxs xxx xxxx xxxxx xxxxxxxxxx xxx xxx xxxx xxx.eml"));
                    Assert.That(Pri.LongPath.File.ReadAllText(download), Is.EqualTo(content));
                }
            }
        }

        [Test]
        public void Verify_basic_integrity_check()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "test1.txt");
            var id = _sut.Upload(DocumentFormats.Original, tempFileName);
            Assert.That(_sut.CheckIntegrity(id), Is.True);

            //we could not black-box testing tampering with the file because
            //we do not exactly know where the content is saved. We are satisfied
            //that the check integrity is ok.
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

            Assert.Throws<Exception>(() => _sut.GetDescriptor(id));
        }

        [Test]
        public void Verify_delete_stream_with_reference()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, $"{Guid.NewGuid()}.txt");
            var id = _sut.UploadReference(DocumentFormats.Original, tempFileName);
            Assert.That(id, Is.Not.Null);
            _sut.Delete(id);

            Assert.Throws<Exception>(() => _sut.GetDescriptor(id));
            Assert.That(File.Exists(tempFileName), "Reference file MUST not be deleted");
            Assert.That(File.ReadAllText(tempFileName), Is.EqualTo(content), "original file should not be modified");
        }

        /// <summary>
        /// To migrate from GridFs we need to pass the id from the outside of the 
        /// blob store
        /// </summary>
        [Test]
        public void Verify_capability_to_store_directly_with_blobId()
        {
            String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId blobId = new BlobId(DocumentFormats.Original, 42);

            var id = _sutAdvanced.Persist(blobId, tempFileName);
            Assert.That(id.BlobId, Is.EqualTo(blobId));

            var descriptor = _sut.GetDescriptor(blobId);
            Assert.That(descriptor.BlobId, Is.EqualTo(blobId));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));

            var download = _sut.Download(blobId, _tempLocalDirectory);
            Assert.That(Path.GetFileName(download), Is.EqualTo("thisisatest.txt"));
            Assert.That(File.ReadAllText(download), Is.EqualTo(content));
        }

        /// <summary>
        /// To migrate from GridFs we need to pass the id from the outside of the 
        /// blob store
        /// </summary>
        [Test]
        public void Verify_capability_to_raw_store()
        {
            String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            var id = _sut.Upload(DocumentFormats.Original, tempFileName);

            var newBlobId = new BlobId(DocumentFormats.Original, id.Id + 1);
            var descriptor = _sut.GetDescriptor(id);

            _sutAdvanced.RawStore(newBlobId, descriptor);

            descriptor = _sut.GetDescriptor(newBlobId);
            Assert.That(descriptor.BlobId, Is.EqualTo(newBlobId));
            Assert.That(descriptor.ContentType, Is.EqualTo("text/plain"));
            Assert.That(descriptor.FileNameWithExtension.ToString(), Is.EqualTo("thisisatest.txt"));
            Assert.That(descriptor.Hash.ToString(), Is.EqualTo("c4afda0ebfa886d489fe06a436ca491a"));
            Assert.That(descriptor.Length, Is.EqualTo(31));

            var download = _sut.Download(newBlobId, _tempLocalDirectory);
            Assert.That(Path.GetFileName(download), Is.EqualTo("thisisatest.txt"));
            Assert.That(File.ReadAllText(download), Is.EqualTo(content));
        }

        [Test]
        public void Can_store_file_as_a_reference()
        {
            const String content = "this is the content of the file";
            const String newContent = "Content of the file is modified";

            BlobId id = CreateABlobReferenceThenAssertWriteAndThenChangeOriginalFile(content, newContent);

            //Verify that the content is changed, this is an indirect verification that the
            //content of the file is indeed the original uploaded file.
            AssertBlobIdContainsSpecificContent(id, newContent);
        }

        [Test]
        public void Can_detect_if_file_content_is_changed()
        {
            const String content = "this is the content of the file";
            const String newContent = "Content of the file is modified";

            BlobId id = CreateABlobReferenceThenAssertWriteAndThenChangeOriginalFile(content, newContent);

            Assert.That(_sut.CheckIntegrity(id), Is.False);
        }

        [Test]
        public void Can_download_reference_file()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id = UploadContentAsReference(content, tempFileName);

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            try
            {
                var downloadedFile = _sut.Download(id, tempPath);

                Assert.That(File.ReadAllText(downloadedFile), Is.EqualTo(content));
            }
            finally
            {
                Directory.Delete(tempPath, true);
            }
        }

        [Test]
        public void Can_read_reference_file_as_stream()
        {
            const String content = "this is the content of the file";
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id = UploadContentAsReference(content, tempFileName);

            var descriptor = _sut.GetDescriptor(id);
            using (var stream = descriptor.OpenRead())
            using (var sr = new StreamReader(stream))
            {
                var readedContent = sr.ReadToEnd();
                Assert.That(readedContent, Is.EqualTo(content));
            }
        }

        private BlobId CreateABlobReferenceThenAssertWriteAndThenChangeOriginalFile(string content, string newContent)
        {
            String tempFileName = GenerateTempTextFile(content, "thisisatest.txt");
            BlobId id = UploadContentAsReference(content, tempFileName);

            //to verify that the storage is indeed pointing to the original file, we change
            //the original file and verify that the content is indeed changed.
            File.WriteAllText(tempFileName, newContent);
            return id;
        }

        private BlobId UploadContentAsReference(string content, string tempFileName)
        {
            BlobId id = _sut.UploadReference(DocumentFormats.Original, tempFileName);

            Assert.That(id, Is.Not.Null);
            Assert.That(File.Exists(tempFileName), "Original file MUST not be deleted");

            AssertBlobIdContainsSpecificContent(id, content);
            return id;
        }

        /// <summary>
        /// Verify that a blob contains a specific storage.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="content"></param>
        private void AssertBlobIdContainsSpecificContent(BlobId id, string content)
        {
            var descriptor = _sut.GetDescriptor(id);
            using (var stream = descriptor.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                String value = reader.ReadToEnd();
                Assert.That(value, Is.EqualTo(content));
            }
        }
    }
}

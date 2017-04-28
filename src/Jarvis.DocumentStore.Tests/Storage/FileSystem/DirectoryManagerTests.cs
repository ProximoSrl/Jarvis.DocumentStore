using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage.FileSystem;
using NUnit.Framework;
using System;
using System.IO;

namespace Jarvis.DocumentStore.Tests.Storage.FileSystem
{
    [TestFixture]
    public class DirectoryManagerTests
    {
        private DirectoryManager _sut;
        private String _tempLocalDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempLocalDirectory = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(_tempLocalDirectory);
            _sut = new DirectoryManager(_tempLocalDirectory, 3);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_tempLocalDirectory, true);
        }

        [TestCase(1, "\\original\\000\\000\\000\\000\\000\\1.filename.pdf")]
        [TestCase(999, "\\original\\000\\000\\000\\000\\000\\999.filename.pdf")]
        [TestCase(1000, "\\original\\000\\000\\000\\000\\001\\1000.filename.pdf")]
        [TestCase(1000000, "\\original\\000\\000\\000\\001\\000\\1000000.filename.pdf")]
        [TestCase(1000000000, "\\original\\000\\000\\001\\000\\000\\1000000000.filename.pdf")]
        [TestCase(1000000000000, "\\original\\000\\001\\000\\000\\000\\1000000000000.filename.pdf")]
        [TestCase(1000000000000000, "\\original\\001\\000\\000\\000\\000\\1000000000000000.filename.pdf")]
        public void Verify_basic_trasformation_from_id_to_filename(Int64 value, string expectedSuffix)
        {
            BlobId id = new BlobId("original." + value);
            var fileName = _sut.GetFileNameFromBlobId(id, "filename.pdf");
            Assert.That(fileName, Is.EqualTo(_tempLocalDirectory + expectedSuffix));
        }

        [TestCase("original", 1, "\\original\\000\\000\\000\\000\\000\\1.filename.pdf")]
        [TestCase("pdf", 1, "\\pdf\\000\\000\\000\\000\\000\\1.filename.pdf")]
        [TestCase("thumb.small", 1, "\\thumb.small\\000\\000\\000\\000\\000\\1.filename.pdf")]
        public void Verify_format_is_used_to_determine_filename(String documentFormat, Int64 value, string expectedSuffix)
        {
            BlobId id = new BlobId($"{documentFormat}.{value}");
            var fileName = _sut.GetFileNameFromBlobId(id, "filename.pdf");
            Assert.That(fileName, Is.EqualTo(_tempLocalDirectory + expectedSuffix));
        }

        [Test]
        public void Verify_ability_to_create_base_directory()
        {
            var notExistingBaseDirectory  = Path.GetTempPath() + Guid.NewGuid().ToString();
            new DirectoryManager(notExistingBaseDirectory, 3);
            Assert.That(Directory.Exists(notExistingBaseDirectory));
        }

        [Test]
        public void Verify_file_name_with_really_large_number()
        {
            BlobId id = new BlobId("original." + Int64.MaxValue);
            Console.WriteLine(Int64.MaxValue);
            var fileName = _sut.GetFileNameFromBlobId(id, "filename.pdf");
            Assert.That(fileName.EndsWith(Int64.MaxValue + ".filename.pdf"));
            Assert.That(fileName, Is.EqualTo(_tempLocalDirectory + "\\original\\922\\337\\203\\685\\477\\9223372036854775807.filename.pdf"));
        }
    }
}

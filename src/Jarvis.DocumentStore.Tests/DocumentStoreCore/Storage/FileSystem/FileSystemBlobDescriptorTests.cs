using Fasterflect;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using NUnit.Framework;
using System;
using System.IO;

namespace Jarvis.DocumentStore.Tests.DocumentStoreCore.Storage.FileSystem
{
    [TestFixture]
    public class FileSystemBlobDescriptorTests
    {
        private string _tempLocalDirectory;
        private Core.Storage.FileSystem.DirectoryManager _directoryManager;

        [OneTimeSetUp]
        public void OneTimneSetup()
        {
            _tempLocalDirectory = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(_tempLocalDirectory);

            _directoryManager = new Core.Storage.FileSystem.DirectoryManager(_tempLocalDirectory, 3);
        }

        [Test]
        public void Verify_add_rooted_name()
        {
            FileSystemBlobDescriptor sut = new FileSystemBlobDescriptor(
                _directoryManager,
                new BlobId(DocumentFormats.Original, 2),
                new FileNameWithExtension("pippo.pdf"),
                DateTime.UtcNow,
                MimeTypes.GetMimeType("pippo.pdf"));

            Assert.That(sut.LocalFileName, Is.EqualTo($"{_tempLocalDirectory}\\original\\000\\000\\000\\000\\000\\2.pippo.pdf"), "Newly created files should have local file name");
        }

        [Test]
        public void Old_descriptor_without_rooted_file_can_be_retrieved()
        {
            FileSystemBlobDescriptor sut = new FileSystemBlobDescriptor(
                _directoryManager,
                new BlobId(DocumentFormats.Original, 2),
                new FileNameWithExtension("pippo.pdf"),
                DateTime.UtcNow,
                MimeTypes.GetMimeType("pippo.pdf"));

            //manually clear property value with reflection to simulate record created with old version, when LocalFileName is not stored.
            sut.SetPropertyValue("LocalFileName", "");
            Assert.That(sut.LocalFileName, Is.Empty);

            //set local file during load.
            sut.SetLocalFileName(_directoryManager);
            Assert.That(sut.LocalFileName, Is.EqualTo($"{_tempLocalDirectory}\\original\\000\\000\\000\\000\\000\\2.pippo.pdf"), "Newly created files should have local file name");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Shared.IdentitySupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ServicesTests
{
    [TestFixture]
    public class GridFsBlobStoreTests
    {
        public class Parent
        {
            public class Child
            {
                public string Value { get; set; }
            }

            public DocumentFormat Format { get; set; }
            public IList<Child> Childs { get; private set; }
            public Parent()
            {
                Childs = new List<Child>();
            }

            public void AddChild(string value)
            {
                this.Childs.Add(new Child { Value = value });
            }
        }

        GridFsBlobStore _fs;

        [SetUp]
        public void SetUp()
        {
            MongoDbTestConnectionProvider.DropTenant1();

            _fs = new GridFsBlobStore
            (
                MongoDbTestConnectionProvider.OriginalsDb.GridFS,
                new CounterService(MongoDbTestConnectionProvider.SystemDb)
            )
            {
                Logger = new ConsoleLogger()
            };
        }

        [Test]
        public void original_blobId_should_be_Original1()
        {
            using (var writer = _fs.CreateNew(DocumentFormats.Original, new FileNameWithExtension("a.file")))
            {
                Assert.AreEqual(new BlobId("original.1"), writer.BlobId);
            }
        }

        [Test]
        public void tika_blobId_should_be_Tika1()
        {
            using (var writer = _fs.CreateNew(DocumentFormats.Tika, new FileNameWithExtension("a.file")))
            {
                Assert.AreEqual(new BlobId("tika.1"), writer.BlobId);
            }
        }

        [Test]
        public void second_tika_blobId_should_be_Tika2()
        {
            using (var writer = _fs.CreateNew(DocumentFormats.Tika, new FileNameWithExtension("a.file")))
            {
            }

            using (var writer = _fs.CreateNew(DocumentFormats.Tika, new FileNameWithExtension("a.file")))
            {
                Assert.AreEqual(new BlobId("tika.2"), writer.BlobId);
            }
        }

        [Test]
        public void should_write_a_csharp_object()
        {
            var o = new Parent();
            o.AddChild("one");
            o.AddChild("two");

            var parentFormat = new DocumentFormat("parent");
            var id = _fs.Save(parentFormat, o);
            Assert.AreEqual(new BlobId(parentFormat, 1), id);

            var descriptor = _fs.GetDescriptor(id);

            Assert.AreEqual(
                new FileNameWithExtension("Parent.json"), 
                descriptor.FileNameWithExtension
            );
        }
    }
}

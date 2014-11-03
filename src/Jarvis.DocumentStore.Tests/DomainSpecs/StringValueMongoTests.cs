using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [TestFixture]
    public class StringValueMongoTests
    {
        [Test]
        public void should_serialize()
        {
            var instance = new ClassWithBlobId{BlobId = new BlobId("abc_123")};
            var json = instance.ToJson();

            Assert.AreEqual("{ \"BlobId\" : \"abc_123\" }", json);
        }

        [Test]
        public void should_deserialize()
        {
            var instance = BsonSerializer.Deserialize<ClassWithBlobId>("{ BlobId:\"abc_123\"}");
            Assert.AreEqual("abc_123", (string) instance.BlobId);
        }

        [Test]
        public void should_serialize_null()
        {
            var instance = new ClassWithBlobId();
            var json = instance.ToJson();
            Assert.AreEqual("{ \"BlobId\" : null }", json);
        }

        [Test]
        public void should_deserialize_null()
        {
            var instance = BsonSerializer.Deserialize<ClassWithBlobId>("{ BlobId: null}");
            Assert.IsNull(instance.BlobId);
        }
    }
}

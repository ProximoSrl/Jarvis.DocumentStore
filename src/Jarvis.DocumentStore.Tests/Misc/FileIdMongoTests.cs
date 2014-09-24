using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture]
    public class FileIdMongoTests
    {
        [Test]
        public void should_serialize()
        {
            var instance = new ClassWithFileId{FileId = new FileId("abc_123")};
            var json = instance.ToJson();

            Assert.AreEqual("{ \"FileId\" : \"abc_123\" }", json);
        }

        [Test]
        public void should_deserialize()
        {
            var instance = BsonSerializer.Deserialize<ClassWithFileId>("{ FileId:\"abc_123\"}");
            Assert.AreEqual("abc_123", (string) instance.FileId);
        }

        [Test]
        public void should_serialize_null()
        {
            var instance = new ClassWithFileId();
            var json = instance.ToJson();
            Assert.AreEqual("{ \"FileId\" : null }", json);
        }

        [Test]
        public void should_deserialize_null()
        {
            var instance = BsonSerializer.Deserialize<ClassWithFileId>("{ FileId: null}");
            Assert.IsNull(instance.FileId);
        }
    }
}

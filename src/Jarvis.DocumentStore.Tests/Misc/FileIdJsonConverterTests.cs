using CQRS.Shared.Domain;
using Jarvis.DocumentStore.Core.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture]
    public class FileIdJsonConverterTests
    {
        JsonSerializerSettings _settings;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _settings = new JsonSerializerSettings()
            {
                Converters = new JsonConverter[]
                {
                    new StringValueJsonConverter()
                }
            };
        }

        [Test]
        public void should_serialize()
        {
            var instance = new ClassWithFileId { FileId = new FileId("abc_123") };
            var json = JsonConvert.SerializeObject(instance, _settings);

            Assert.AreEqual("{\"FileId\":\"abc_123\"}", json);
        }

        [Test]
        public void should_deserialize()
        {
            var instance = JsonConvert.DeserializeObject<ClassWithFileId>("{ FileId:\"abc_123\"}",_settings);
            Assert.AreEqual("abc_123", (string)instance.FileId);
        }

        [Test]
        public void should_serialize_null()
        {
            var instance = new ClassWithFileId();
            var json = JsonConvert.SerializeObject(instance, _settings);
            Assert.AreEqual("{\"FileId\":null}", json);
        }

        [Test]
        public void should_deserialize_null()
        {
            var instance = JsonConvert.DeserializeObject<ClassWithFileId>("{ FileId:null}", _settings);
            Assert.IsNull(instance.FileId);
        }
    
    }
}
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.Misc;
using Jarvis.Framework.Shared.Domain.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [TestFixture]
    public class BlobIdJsonConverterTests
    {
        JsonSerializerSettings _settings;

        [OneTimeSetUp]
        public void OneTimeSetUp()
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
            var instance = new ClassWithBlobId { BlobId = new BlobId("abc_123") };
            var json = JsonConvert.SerializeObject(instance, _settings);

            Assert.AreEqual("{\"BlobId\":\"abc_123\"}", json);
        }

        [Test]
        public void should_deserialize()
        {
            var instance = JsonConvert.DeserializeObject<ClassWithBlobId>("{ BlobId:\"abc_123\"}",_settings);
            Assert.AreEqual("abc_123", (string)instance.BlobId);
        }

        [Test]
        public void should_serialize_null()
        {
            var instance = new ClassWithBlobId();
            var json = JsonConvert.SerializeObject(instance, _settings);
            Assert.AreEqual("{\"BlobId\":null}", json);
        }

        [Test]
        public void should_deserialize_null()
        {
            var instance = JsonConvert.DeserializeObject<ClassWithBlobId>("{ BlobId:null}", _settings);
            Assert.IsNull(instance.BlobId);
        }
    
    }
}
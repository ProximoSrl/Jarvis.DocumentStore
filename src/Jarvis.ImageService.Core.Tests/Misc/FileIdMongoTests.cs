using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.Misc
{
    public class ClassWithFileId
    {
        public FileId FileId { get; set; }
    }

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
                    new FileIdJsonConverter()
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

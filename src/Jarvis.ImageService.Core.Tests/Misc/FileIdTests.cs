using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.Misc
{
    [TestFixture]
    public class FileIdTests
    {
        [Test]
        public void mongo_should_serialize()
        {
            var id = new ImageInfo(new FileId("abc_123"),"temp.txt");
            var json = id.ToJson();

            Assert.AreEqual("{ \"_id\" : \"abc_123\", \"Filename\" : \"temp.txt\", \"Sizes\" : { } }", json);
        }

        [Test]
        public void mongo_should_deserialize()
        {
            var imageInfo = BsonSerializer.Deserialize<ImageInfo>("{ _id:\"abc_123\"}");
            Assert.AreEqual("abc_123", (string) imageInfo.Id);
        }
    }
}

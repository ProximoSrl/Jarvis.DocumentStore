using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.Misc
{
    [TestFixture]
    public class SizeInfoTests
    {
        [Test]
        public void name_should_be_converted_to_lowercase()
        {
            var si = new SizeInfo("SmAlL", 100, 100);
            Assert.AreEqual("small", si.Name);
        }
    }

    [TestFixture]
    public class SizeInfoHelperTests
    {
        [Test]
        public void should_serialize_size_info()
        {
            var sizes = new SizeInfo[] {new SizeInfo("SmAll", 100, 200)};
            var asString = SizeInfoHelper.Serialize(sizes);

            Assert.AreEqual("small:100x200", asString);
        }

        [Test]
        public void should_serialize_size_info_list_with_more_than_one_element()
        {
            var sizes = new SizeInfo[]
            {
                new SizeInfo("SmAll", 100, 200),
                new SizeInfo("LARGE", 100, 200)
            };
            var asString = SizeInfoHelper.Serialize(sizes);

            Assert.AreEqual("small:100x200|large:100x200", asString);
        }

        [Test]
        public void should_deserialize_size_info_list_with_more_than_one_element()
        {
            var sizes = SizeInfoHelper.Deserialize("small:100x200|large:110x220");

            Assert.AreEqual(2, sizes.Count());
            Assert.AreEqual("small", sizes[0].Name);
            Assert.AreEqual(100, sizes[0].Width);
            Assert.AreEqual(200, sizes[0].Height);
            Assert.AreEqual("large", sizes[1].Name);
            Assert.AreEqual(110, sizes[1].Width);
            Assert.AreEqual(220, sizes[1].Height);
        }
    }
}

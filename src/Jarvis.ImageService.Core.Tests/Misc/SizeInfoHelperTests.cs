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
    }
}

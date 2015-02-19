using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Jobs.ImageResizer;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture]
    public class SizeInfoHelperTests
    {
        [Test]
        public void should_serialize_size_info()
        {
            var sizes = new ImageSizeInfo[] {new ImageSizeInfo("SmAll", 100, 200)};
            var asString = SizeInfoHelper.Serialize(sizes);

            Assert.AreEqual("small:100x200", asString);
        }

        [Test]
        public void should_serialize_size_info_list_with_more_than_one_element()
        {
            var sizes = new ImageSizeInfo[]
            {
                new ImageSizeInfo("SmAll", 100, 200),
                new ImageSizeInfo("LARGE", 100, 200)
            };
            var asString = SizeInfoHelper.Serialize(sizes);

            Assert.AreEqual("small:100x200|large:100x200", asString);
        }

        [Test]
        public void should_deserialize_size_info_list_with_more_than_one_element()
        {
            var sizes = SizeInfoHelper.Deserialize("small:100x200|large:110x220");

            Assert.AreEqual(2, sizes.Length);
            Assert.AreEqual("small", sizes[0].Name);
            Assert.AreEqual(100, sizes[0].Width);
            Assert.AreEqual(200, sizes[0].Height);
            Assert.AreEqual("large", sizes[1].Name);
            Assert.AreEqual(110, sizes[1].Width);
            Assert.AreEqual(220, sizes[1].Height);
        }
    }
}

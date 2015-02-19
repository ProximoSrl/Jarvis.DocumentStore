using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Jobs.ImageResizer;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture]
    public class SizeInfoTests
    {
        [Test]
        public void name_should_be_converted_to_lowercase()
        {
            var si = new ImageSizeInfo("SmAlL", 100, 100);
            Assert.AreEqual("small", si.Name);
        }
    }
}
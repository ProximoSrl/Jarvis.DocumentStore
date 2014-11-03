using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture]
    public class FileNameWithExtensionTests
    {
        public class ClassWithFileName
        {
            public FileNameWithExtension File { get; set; }
        }

        [Test]
        public void should_serialize()
        {
            var ob = new ClassWithFileName { File = new FileNameWithExtension("Path to.file") };
            var json = JsonConvert.SerializeObject(ob);

            Assert.AreEqual("{\"File\":{\"FileName\":\"Path to\",\"Extension\":\"file\"}}", json);
        }

        [Test]
        public void should_deserialize()
        {
            var json = "{\"File\":{\"FileName\":\"Path to\",\"Extension\":\"file\"}}";
            var ob = JsonConvert.DeserializeObject<ClassWithFileName>(json);

            Assert.NotNull(ob);
            Assert.NotNull(ob.File);
            Assert.AreEqual("file", ob.File.Extension);
            Assert.AreEqual("Path to", ob.File.FileName);
        }

        [Test]
        public void should_handle_extension_with_dots_in_file_name()
        {
            var fname = new FileNameWithExtension("a.b.c");
            Assert.AreEqual("a.b", fname.FileName);
            Assert.AreEqual("c", fname.Extension);
            Assert.AreEqual("a.b.c", (string)fname);

        }
    }
}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using MongoDB.Bson;
using Jarvis.Framework.Shared.Domain;

namespace Jarvis.DocumentStore.Tests.Misc
{
    public class DocumentFormat2 : StringValue
    {
        public DocumentFormat2(string value)
            : base(value)
        {
        }

       
    }

    [TestFixture]
    public class FlatMappingSerializationTest
    {
        public class ClassWithDocumentFormat
        {
            public DocumentFormat2 Value { get; set; }
        }

        [Test]
        public void verify_basic_serialization()
        {
            var instance = new ClassWithDocumentFormat { Value = new DocumentFormat2("Sample_1") };
            var json = instance.ToJson();

            Assert.AreEqual("{ \"Value\" : \"Sample_1\" }", json);
        }


        [Test]
        public void verify_null_serialization()
        {
            var instance = new ClassWithDocumentFormat { Value = null };
            var json = instance.ToJson();

            Assert.AreEqual("{ \"Value\" : null }", json);
        }

        [Test]
        public void verify_null_stringValue_serialization()
        {
            var instance = new ClassWithDocumentFormat { Value = new DocumentFormat2(null) };
            var json = instance.ToJson();

            Assert.AreEqual("{ \"Value\" : null }", json);
        }
    }
}

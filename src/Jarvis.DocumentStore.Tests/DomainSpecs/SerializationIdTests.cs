using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [TestFixture]
    public class SerializationIdTests
    {

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            MongoFlatMapper.EnableFlatMapping();
        }

        [Test]
        public void Serialize_queued_job_id_with_tenant_id()
        {
            QueuedJob job = new QueuedJob();
            job.TenantId = new TenantId("TEST_TENANT");
            job.Id = new QueuedJobId("some id");
            var json = job.ToJson();
            Assert.That(json, Text.Contains("some id"));
            Assert.That(json, Text.Contains("test_tenant"));
        }


    }
}

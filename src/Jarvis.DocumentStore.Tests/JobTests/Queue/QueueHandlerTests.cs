using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Tests.Support;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Core.Domain.Document;
using MongoDB.Bson;
using Jarvis.DocumentStore.Core.Domain.Handle;

namespace Jarvis.DocumentStore.Tests.JobTests.Queue
{
    [TestFixture]
    public class QueueHandlerTests
    {
        MongoDatabase _db = MongoDbTestConnectionProvider.QueueDb;

        [SetUp]
        public void SetUp()
        {
            _db.Drop();
        }

        [Test]
        public void verify_file_extension_on_handler_filter_exact_extension()
        {
            var info = new QueueInfo("test", "", "pdf|doc");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx")
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue-test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(0));

        }

        [Test]
        public void verify_file_extension_permitted()
        {
            var info = new QueueInfo("test", "", "pdf|docx");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.HandleHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("soffice"),
                    DocumentFormat = new DocumentFormat("office"),
                    BlobId = new BlobId("soffice.1")
                }
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1));
        }

        [Test]
        public void verify_pipeline_id_filter()
        {
            var info = new QueueInfo("test", "tika", "");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.HandleHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("soffice")
                }
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(0), "pipeline filter is not filtering out unwanted pipeline");

            rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("tika"),
                    DocumentFormat = new DocumentFormat("tika"),
                    BlobId = new BlobId("tika.1")
                }
            };
            sut.Handle(rm, new TenantId("test"));

            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1), "pipeline filter is not filtering in admitted pipeline");

        }

        [Test]
        public void verify_get_next_job_set_identity()
        {
            var info = new QueueInfo("test", "", "pdf|docx");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Id = 1L,
                Handle = "FirstHandle",
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.HandleHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("soffice"),
                    DocumentFormat = new DocumentFormat("office"),
                    BlobId = new BlobId("soffice.1")
                },
                DocumentId = new DocumentId(1),
            };

            sut.Handle(rm, new TenantId("test"));
            rm.Handle = "SecondHandle";
            rm.Id = 2L;
            //This is typical situation when handle is de-duplicated, because
            //and handle is assigned to another document, but the underling blob id is the same.
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            //no need to schedule another job
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1));
        }

        [Test]
        public void verify_job_created_with_handle_metadata()
        {
            var info = new QueueInfo("test", "", "pdf|docx");
            QueueHandler sut = new QueueHandler(info, _db);
            var customData = new Core.Domain.Handle.HandleCustomData() 
                {
                    {"test" , "value"},
                    {"complex" , 42},
                };
            StreamReadModel rm = new StreamReadModel()
            {
                Id = 1L,
                Handle = "FirstHandle",
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.HandleHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("soffice"),
                    DocumentFormat = new DocumentFormat("office"),
                    BlobId = new BlobId("soffice.1")
                },
                DocumentId = new DocumentId(1),
                HandleCustomData = customData,
            };

            sut.Handle(rm, new TenantId("test"));

            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Single().HandleCustomData, Is.EquivalentTo(customData));
        }

        [Test]
        public void verify_not_duplicate_jobs_on_same_blob_id()
        {
            QueueHandler sut = CreateAGenericJob(new QueueInfo("test", "tika", ""));
            var nextJob = sut.GetNextJob("identity", "handle", null, null);
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            var job = collection.FindOneById(BsonValue.Create(nextJob.Id));
            Assert.That(job.ExecutingIdentity, Is.EqualTo("identity"));
        }

        [Test]
        public void verify_get_next_job_not_give_executing_job()
        {
            QueueHandler sut = CreateAGenericJob(new QueueInfo("test", "tika", ""));
            var nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Not.Null);
            nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Null);
        }

        [Test]
        public void verify_max_number_of_falure()
        {
            var info = new QueueInfo("test", "tika", "");
            info.MaxNumberOfFailure = 2;
            QueueHandler sut = CreateAGenericJob(info);

            var nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Not.Null);
            var jobId = nextJob.Id;

            sut.SetJobExecuted(nextJob.Id, "Error 42");
            nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Not.Null);
            sut.SetJobExecuted(nextJob.Id, "Error 42");
            nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Null, "After two failure the job should not be returned anymore");

            var collection = _db.GetCollection<QueuedJob>("queue.test");
            var job = collection.FindOneById(BsonValue.Create(jobId));
            Assert.That(job.ExecutionError, Is.EqualTo("Error 42"));
            Assert.That(job.ErrorCount, Is.EqualTo(2));
            Assert.That(job.Status, Is.EqualTo(QueuedJobExecutionStatus.Failed));
        }

        [Test]
        public void verify_job_is_generated_with_custom_parameters()
        {
            var info = new QueueInfo("test", "tika", "");
            info.Parameters = new Dictionary<string, string>() { { "Custom", "CustomValue" } };
            QueueHandler sut = CreateAGenericJob(info);

            var nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob.Parameters["Custom"], Is.EqualTo("CustomValue"));

        }

        [Test]
        public void verify_set_error_status()
        {
            var info = new QueueInfo("test", "tika", "");
            info.MaxNumberOfFailure = 2;
            QueueHandler sut = CreateAGenericJob(info);
            var nextJob = sut.GetNextJob("", "handle", null, null);
            sut.SetJobExecuted(nextJob.Id, "Error 42");
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            var job = collection.FindOneById(BsonValue.Create(nextJob.Id));
            Assert.That(job.ExecutionError, Is.EqualTo("Error 42"));
            Assert.That(job.ErrorCount, Is.EqualTo(1));
            Assert.That(job.Status, Is.EqualTo(QueuedJobExecutionStatus.ReQueued));
        }

        [Test]
        public void verify_job_filter_by_tenant_id()
        {
            var none = new TenantId("tenant_none");
            var foo = new TenantId("tenant_foo");
            var bar = new TenantId("tenant_bar");
            QueueHandler sut = CreateAGenericJob(new QueueInfo("test", "tika", ""), tenant: foo);
            HandleStreamToCreateJob(sut, bar);
            var nextJob = sut.GetNextJob("identity", "handle",none , null);
            Assert.That(nextJob, Is.Null);
            nextJob = sut.GetNextJob("identity", "handle", foo, null);
            Assert.That(nextJob.TenantId, Is.EqualTo(foo.ToString()));
            nextJob = sut.GetNextJob("identity", "handle", bar, null);
            Assert.That(nextJob.TenantId, Is.EqualTo(bar.ToString()));
        }

        private QueueHandler GetSut(QueueInfo info)
        {
            return new QueueHandler(info, _db);
        }

        private QueueHandler CreateAGenericJob(QueueInfo info, String tenant = "test", Dictionary<String, Object> customData = null)
        {
            QueueHandler sut = GetSut(info);
            HandleStreamToCreateJob(sut, tenant, customData);
            return sut;
        }

        private static void HandleStreamToCreateJob(QueueHandler sut, String tenant = "test", Dictionary<String, Object> customData = null)
        {
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.HandleHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("tika"),
                    DocumentFormat = new DocumentFormat("tika"),
                    BlobId = new BlobId("tika.1")
                },
                HandleCustomData = new HandleCustomData(customData ?? new Dictionary<String,Object>()),
            };
            sut.Handle(rm, new TenantId(tenant));
        }
    }
}

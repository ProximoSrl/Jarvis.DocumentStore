using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Core.Domain.Document;
using MongoDB.Bson;
using Jarvis.DocumentStore.Core.Jobs;
using MongoDB.Bson.Serialization;
using Jarvis.DocumentStore.Core;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.Framework.Shared.Helpers;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;

namespace Jarvis.DocumentStore.Tests.JobTests.Queue
{
    [TestFixture]
    public class QueueHandlerTests
    {
        IMongoDatabase _db = MongoDbTestConnectionProvider.QueueDb;


        [SetUp]
        public void SetUp()
        {
            _db.Drop();

            var mngr = new IdentityManager(new CounterService(MongoDbTestConnectionProvider.ReadModelDb));
            mngr.RegisterIdentitiesFromAssembly(typeof(DocumentDescriptorId).Assembly);
            MongoFlatIdSerializerHelper.Initialize(mngr);
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
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(0));
        }

        /// <summary>
        /// Pristine version of queue used blob id and tenant id as id of the job
        /// this is not permitted because the id should be completely opaque.
        /// </summary>
        [Test]
        public void verify_id_is_opaque_and_not_contains_blob_id()
        {
            var info = new QueueInfo("test", "", "docx");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                FormatInfo = new FormatInfo()
                {
                    DocumentFormat = new DocumentFormat("thumb.small"),
                    BlobId = new BlobId("blob.1"),
                    PipelineId = new PipelineId("thumbnail"),
                },
                DocumentDescriptorId = new DocumentDescriptorId(1),
                Handle = new DocumentHandle("Revision_2"),
            };
            sut.Handle(rm, new TenantId("test_tenant"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1));
            var job = collection.AsQueryable().Single();
            Assert.That(job.BlobId, Is.EqualTo(new BlobId("blob.1")));
            Assert.That(job.TenantId, Is.EqualTo(new TenantId("test_tenant")));
            Assert.That(job.DocumentDescriptorId, Is.EqualTo(new DocumentDescriptorId(1)));
            Assert.That(job.Handle.ToString(), Is.EqualTo(rm.Handle));
            Assert.That(job.Id.ToString(), Is.Not.Contains("blob.1"), "Id should not contains internal concempts like blob id");
            Assert.That(job.Id.ToString(), Is.Not.Contains("tenant"), "Id should not contains internal concempts like tenant id");
            Assert.That(job.Parameters.Keys, Is.Not.Contains(JobKeys.BlobId));
            Assert.That(job.Parameters.Keys, Is.Not.Contains(JobKeys.DocumentId));
        }

        /// <summary>
        /// </summary>
        [Test]
        public void verify_job_parameters_contains_mime_type()
        {
            var info = new QueueInfo("test", "", "docx");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                FormatInfo = new FormatInfo()
                {
                    DocumentFormat = new DocumentFormat("thumb.small"),
                    BlobId = new BlobId("blob.1"),
                    PipelineId = new PipelineId("thumbnail"),
                },
                DocumentDescriptorId = new DocumentDescriptorId(1),
            };
            sut.Handle(rm, new TenantId("test_tenant"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1));
            var job = collection.AsQueryable().Single();
            Assert.That(job.BlobId, Is.EqualTo(new BlobId("blob.1")));
            Assert.That(job.Parameters[JobKeys.MimeType], Is.EqualTo(MimeTypes.GetMimeTypeByExtension("docx")));
        }

        [Test]
        public void verify_filtering_on_blob_format()
        {
            var info = new QueueInfo("test", "", "", "rasterimage");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                FormatInfo = new FormatInfo()
                {
                    DocumentFormat = new DocumentFormat("thumb.small"),
                    BlobId = new BlobId("blob.1"),
                    PipelineId = new PipelineId("thumbnail")
                }
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(0));
        }

        [Test]
        public void verify_filtering_on_mime_types()
        {
            var mimeTypeDocx = MimeTypes.GetMimeTypeByExtension("docx");
            var info = new QueueInfo("test", mimeTypes: mimeTypeDocx);
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.pdf"),
                FormatInfo = new FormatInfo()
                {
                    DocumentFormat = new DocumentFormat("thumb.small"),
                    BlobId = new BlobId("blob.1"),
                    PipelineId = new PipelineId("thumbnail")
                }
            };
            sut.Handle(rm, new TenantId("test"));
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(0));

            rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                FormatInfo = new FormatInfo()
                {
                    DocumentFormat = new DocumentFormat("thumb.small"),
                    BlobId = new BlobId("blob.1"),
                    PipelineId = new PipelineId("thumbnail")
                }
            };
            sut.Handle(rm, new TenantId("test"));

            Assert.That(collection.AsQueryable().Count(), Is.EqualTo(1));
        }

        [Test]
        public void verify_file_extension_permitted()
        {
            var info = new QueueInfo("test", "", "pdf|docx");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.DocumentHasNewFormat,
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
                EventType = HandleStreamEventTypes.DocumentHasNewFormat,
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
                EventType = HandleStreamEventTypes.DocumentHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("soffice"),
                    DocumentFormat = new DocumentFormat("office"),
                    BlobId = new BlobId("soffice.1")
                },
                DocumentDescriptorId = new DocumentDescriptorId(1),
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
            var customData = new DocumentCustomData()
                {
                    {"test" , "value"},
                    {"complex" , 42},
                };
            StreamReadModel rm = new StreamReadModel()
            {
                Id = 1L,
                Handle = "FirstHandle",
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.DocumentHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("soffice"),
                    DocumentFormat = new DocumentFormat("office"),
                    BlobId = new BlobId("soffice.1")
                },
                DocumentDescriptorId = new DocumentDescriptorId(1),
                DocumentCustomData = customData,
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
            var job = collection.Find(Builders<QueuedJob>.Filter.Eq(j => j.Id, nextJob.Id)).SingleOrDefault();
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

            sut.SetJobExecuted(nextJob.Id, "Error 42", null);
            nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Not.Null);
            sut.SetJobExecuted(nextJob.Id, "Error 42", null);
            nextJob = sut.GetNextJob("", "handle", null, null);
            Assert.That(nextJob, Is.Null, "After two failure the job should not be returned anymore");

            var collection = _db.GetCollection<QueuedJob>("queue.test");
            var job = collection.Find(Builders<QueuedJob>.Filter.Eq(j => j.Id, jobId)).SingleOrDefault();
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
            sut.SetJobExecuted(nextJob.Id, "Error 42", null);
            var collection = _db.GetCollection<QueuedJob>("queue.test");
            var job = collection.Find(Builders<QueuedJob>.Filter.Eq(j => j.Id, nextJob.Id)).SingleOrDefault();
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
            var nextJob = sut.GetNextJob("identity", "handle", none, null);
            Assert.That(nextJob, Is.Null);
            nextJob = sut.GetNextJob("identity", "handle", bar, null);
            Assert.That(nextJob.TenantId, Is.EqualTo(bar));
            nextJob = sut.GetNextJob("identity", "handle", foo, null);
            Assert.That(nextJob.TenantId, Is.EqualTo(foo));
        }

        [Test]
        public void verify_job_filter_by_custom_properties()
        {
            QueueHandler sut = CreateAGenericJob(new QueueInfo("test", "tika", ""),
                customData: new Dictionary<String, Object>()
                {
                    {"foo" , 6},
                    {"bar" , "test"},
                });
            HandleStreamToCreateJob(sut,
                customData: new Dictionary<String, Object>()
                {
                    {"foo" , 42},
                    {"bar" , "the ultimate answer"},
                });
            var nextJob = sut.GetNextJob("identity", "handle", null, new Dictionary<string, Object>() { { "notexisting", 11 } });
            Assert.That(nextJob, Is.Null);
            nextJob = sut.GetNextJob("identity", "handle", null, new Dictionary<string, Object>() { { "foo", 42 } });
            Assert.That(nextJob.HandleCustomData["bar"], Is.EqualTo("the ultimate answer"));
            nextJob = sut.GetNextJob("identity", "handle", null, new Dictionary<string, Object>() { { "foo", 6 } });
            Assert.That(nextJob.HandleCustomData["bar"], Is.EqualTo("test"));
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
        private static Int32 lastBlobId = 1;
        private static void HandleStreamToCreateJob(
            QueueHandler sut,
            String tenant = "test",
            Dictionary<String, Object> customData = null)
        {
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.DocumentHasNewFormat,
                FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("tika"),
                    DocumentFormat = new DocumentFormat("tika"),
                    BlobId = new BlobId("tika." + lastBlobId++)
                },
                DocumentCustomData = new DocumentCustomData(customData ?? new Dictionary<String, Object>()),
            };
            sut.Handle(rm, new TenantId(tenant));
        }
    }
}

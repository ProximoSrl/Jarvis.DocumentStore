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

namespace Jarvis.DocumentStore.Tests.JobTests.Queue
{
    [TestFixture]
    public class QueueHandlerTests
    {
        MongoDatabase _db = MongoDbTestConnectionProvider.ReadModelDb;

        [SetUp]
        public void SetUp() 
        {
            _db.Drop();
        }

        [Test]
        public void verify_file_extension_on_handler_filter_exact_extension()
        {
            var info = new QueueInfo("test", "", "pdf|doc" ) ;
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
        public void verify_get_next_job_not_give_executing_job()
        {
            var info = new QueueInfo("test", "tika", "");
            QueueHandler sut = new QueueHandler(info, _db);
            StreamReadModel rm = new StreamReadModel()
            {
                Filename = new FileNameWithExtension("test.docx"),
                EventType = HandleStreamEventTypes.HandleHasNewFormat,
                 FormatInfo = new FormatInfo()
                {
                    PipelineId = new PipelineId("tika"),
                    DocumentFormat = new DocumentFormat("tika"),
                    BlobId = new BlobId("tika.1")
                }
            };
            sut.Handle(rm, new TenantId("test"));

            var nextJob = sut.GetNextJob();
            Assert.That(nextJob, Is.Not.Null);
            nextJob = sut.GetNextJob();
            Assert.That(nextJob, Is.Null);

        }

        [Test]
        public void verify_job_is_generated_with_custom_parameters()
        {
            var info = new QueueInfo("test", "tika", "");
            info.Parameters = new Dictionary<string, string>() { {"Custom", "CustomValue"} };
            QueueHandler sut = new QueueHandler(info, _db);

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
            };
            sut.Handle(rm, new TenantId("test"));

            var nextJob = sut.GetNextJob();
            Assert.That(nextJob.Parameters["Custom"], Is.EqualTo("CustomValue"));

        }
    }
}

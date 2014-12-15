using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture]
    public class JobHelperTests
    {
        JobHelper sut;
        IScheduler _scheduler;

        [SetUp]
        public void SetUp() 
        {
            _scheduler = StdSchedulerFactory.GetDefaultScheduler();
            sut = new JobHelper(_scheduler, new Core.Services.ConfigService());
        }

        [TearDown]
        public void TearDown() 
        {
            _scheduler.Clear();
        }

        [Test]
        public void verify_job_helper_is_capable_of_scheduling_two_times_the_job()
        {
            //Just verify that no exception is thrown shceduling two time the libre to office conversion
            sut.QueueLibreOfficeToPdfConversion(new PipelineId("test"), new DocumentId(1), new BlobId("TESTBlob1"));
            sut.QueueLibreOfficeToPdfConversion(new PipelineId("test"), new DocumentId(1), new BlobId("TESTBlob1"));
        }

       
    }
}

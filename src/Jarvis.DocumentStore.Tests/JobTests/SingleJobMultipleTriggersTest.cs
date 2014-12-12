using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Quartz;
using Quartz.Core;
using Quartz.Impl;
using System.Threading;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture(Category = "jobs")]
    public class SingleJobMultipleTriggersTest : AbstractJobTest
    {

        [Test]
        public void verify_single_job_with_multiple_trigger_works()
        {
            ConfigureGetFile("doc", TestConfig.PathToDocumentPdf);

            var job = JobBuilder
                .Create<CreateThumbnailFromPdfJob>()
                .WithIdentity(JobKey.Create(Guid.NewGuid().ToString(), typeof(CreateThumbnailFromPdfJob).Name))
                .RequestRecovery(true)
                .StoreDurably(true)
                .Build();
            
            var trigger1 = TriggerBuilder.Create()
                .StartAt(DateTime.Now)
                .UsingJobData(JobKeys.TenantId, TestConfig.Tenant)
                .UsingJobData(JobKeys.DocumentId, "Document_1")
                .UsingJobData(JobKeys.BlobId, "blob.1")
                .UsingJobData(JobKeys.FileExtension, "png")
                .ForJob(job)
                .Build();

            var trigger2 = TriggerBuilder.Create()
               .StartAt(DateTime.Now)
               .UsingJobData(JobKeys.TenantId, TestConfig.Tenant)
               .UsingJobData(JobKeys.DocumentId, "Document_2")
               .UsingJobData(JobKeys.BlobId, "blob.2")
               .UsingJobData(JobKeys.FileExtension, "png")
               .ForJob(job)
               .Build();

            var concreteJob = BuildJob<CreateThumbnailFromPdfJob>();
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = new JobFactoryHelper(concreteJob);
            scheduler.ScheduleJob(job, trigger1);
            scheduler.ScheduleJob(trigger2);
            scheduler.Start();

            TriggerState td1, td2;

            do
            {
                Thread.Sleep(300);
                td1 = scheduler.GetTriggerState(trigger1.Key);
                td2 = scheduler.GetTriggerState(trigger2.Key);

            } while (td1 != TriggerState.None || td2 != TriggerState.None);
           
            var calls = BlobStore.ReceivedCalls();
            var getDescriptorCalls = calls.Where(c => c.GetMethodInfo().Name == "GetDescriptor");
            Assert.That(getDescriptorCalls.Count(), Is.EqualTo(2));
            Assert.That(getDescriptorCalls.Select(c => c.GetArguments().First().ToString()), Is.EquivalentTo(new String[] {"blob.1", "blob.2"}));
        }

        private class JobFactoryHelper : IJobFactory
        {

            private IJob _job;

            public JobFactoryHelper(IJob job)
            {
                _job = job;
            }
            public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
            {
                return _job;
            }

            public void ReturnJob(IJob job)
            {

            }
        }
      
    }
}

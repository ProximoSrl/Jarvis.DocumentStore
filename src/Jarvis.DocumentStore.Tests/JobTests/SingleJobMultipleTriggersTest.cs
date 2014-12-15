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
using Jarvis.DocumentStore.Core.Support;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.Tests.Support;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Services;

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

        [Test]
        public void verify_single_job_with_multiple_trigger_and_TeantJobFactory()
        {
            ConfigureGetFile("doc", TestConfig.PathToDocumentPdf);

            var job = JobBuilder
                .Create<TestJob>()
                .WithIdentity(JobKey.Create(Guid.NewGuid().ToString(), typeof(TestJob).Name))
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

            ITenantAccessor taccessor = NSubstitute.Substitute.For<ITenantAccessor>();
            ITenant tenant= NSubstitute.Substitute.For<ITenant>();
            ConfigService configService = new ConfigService();
            
            taccessor.GetTenant(Arg.Any<TenantId>()).Returns(tenant);
            IBlobStore blobStore = NSubstitute.Substitute.For<IBlobStore>();
            IWindsorContainer container = new WindsorContainer();
            container.Register(
                Component.For<TestJob, TestJob>(),
                Component.For<ILogger>().Instance(new ExtendedConsoleLogger("TestLogger")),
                Component.For<ITenantAccessor>().Instance(taccessor),
                Component.For<ConfigService>().Instance(configService),
                Component.For<IBlobStore>().Instance(blobStore)
            );
            tenant.Container.Returns(container);
            var jobFactory = new TenantJobFactory(container.Kernel);
            jobFactory.Logger = container.Resolve<ILogger>();

            var jobConcrete = container.Resolve<TestJob>();

            var concreteJob = BuildJob<TestJob>();
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = jobFactory;
            scheduler.ScheduleJob(job, trigger1);
            scheduler.ScheduleJob(trigger2);
            scheduler.Start();

            TriggerState td1, td2;

            do
            {
                Thread.Sleep(300);
                td1 = scheduler.GetTriggerState(trigger1.Key);
                td2 = scheduler.GetTriggerState(trigger2.Key);

                if (td1 == TriggerState.Error) Assert.Fail("Job 1 gone in blocked state");
                if (td2 == TriggerState.Error) Assert.Fail("Job 2 gone in blocked state");

                Console.WriteLine("Job 1 status {0} Job 2 status {1}", td1, td2);
            } while (td1 != TriggerState.None || td2 != TriggerState.None);


            Assert.That(TestJob.JobdataMapKeys.Count(), Is.EqualTo(2));
            Assert.That(TestJob.JobdataMapKeys, Is.EquivalentTo(new String[] { "blob.1", "blob.2" }));
        }

        [DisallowConcurrentExecution]
        public class TestJob : AbstractFileJob
        {
            public static List<String> JobdataMapKeys { get; set; }

            static TestJob() 
            {
                JobdataMapKeys = new List<String>(); 
            }

            protected override void OnExecute(IJobExecutionContext context)
            {
                var jobDataMap = context.MergedJobDataMap;
                JobdataMapKeys.Add(jobDataMap.GetString(JobKeys.BlobId));
            }
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

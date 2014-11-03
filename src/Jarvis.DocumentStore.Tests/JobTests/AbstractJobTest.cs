using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Calendar;
using Quartz.Impl.Triggers;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    public abstract class AbstractJobTest
    {
        protected IBlobStore BlobStore;
        protected ICommandBus CommandBus;

        protected IJobExecutionContext BuildContext(IJob job, IEnumerable<KeyValuePair<string, object>> map = null)
        {
            var scheduler = NSubstitute.Substitute.For<IScheduler>();
            var firedBundle = new TriggerFiredBundle(
                new JobDetailImpl("job", job.GetType()),
                new SimpleTriggerImpl("trigger"),
                new AnnualCalendar(),
                false,
                null, null, null, null
                );

            if (map != null)
            {
                foreach (var kvp in map)
                {
                    firedBundle.JobDetail.JobDataMap.Add(kvp);
                }
            }

            return new JobExecutionContextImpl(scheduler, firedBundle, job);
        }

        protected IJobExecutionContext BuildContext(IJob job, object keyValMap)
        {
            var dictionary = keyValMap.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(keyValMap));
            return BuildContext(job, dictionary);
        }

        [SetUp]
        public void SetUp()
        {
            BlobStore = Substitute.For<IBlobStore>();
            CommandBus = Substitute.For<ICommandBus>();
        }

        protected T BuildJob<T>() where T : AbstractFileJob, new()
        {
            var job = new T()
            {
                CommandBus = CommandBus,
                BlobStore = BlobStore,
                Logger = new ConsoleLogger(),
                ConfigService = new ConfigService(),
                TenantId = new TenantId(TestConfig.Tenant)
            };

            return job;
        }

        protected void ConfigureGetFile(string blobId, string pathToFile)
        {
            var id = new BlobId(blobId);
            BlobStore
                .GetDescriptor(id)
                .Returns(new FsFileStoreDescriptor(id,pathToFile));
        }

        protected void ConfigureFileDownload(string blobId, string pathToFile, Action<string> action = null)
        {
            if(action == null)
                action = s => { };

            BlobStore
                .Download(new BlobId(blobId), Arg.Do( action ))
                .Returns(info =>
                {
                    string tmpFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(pathToFile));
                    if(File.Exists(tmpFileName))
                        File.Delete(tmpFileName);

                    File.Copy(pathToFile, tmpFileName);
                    return tmpFileName;
                });
        }

        protected void ExpectFileUpload(Action<string> action = null)
        {
            if(action == null)
                action = s => { };

            var id = BlobStore.Upload(Arg.Any<DocumentFormat>(), Arg.Do(action));
        }

        protected void CaptureCommand(Action<ICommand> action)
        {
            CommandBus.Send(Arg.Do(action));
        }
    }
}
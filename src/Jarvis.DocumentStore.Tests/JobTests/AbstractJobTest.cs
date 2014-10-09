using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
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
        protected IFileStore FileStore;
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
            FileStore = Substitute.For<IFileStore>();
            CommandBus = Substitute.For<ICommandBus>();
        }

        protected T BuildJob<T>() where T : AbstractFileJob, new()
        {
            var job = new T()
            {
                CommandBus = CommandBus,
                FileStore = FileStore,
                Logger = new ConsoleLogger(),
                ConfigService = new ConfigService()
            };

            return job;
        }

        protected void ConfigureGetFile(string fileId, string pathToFile)
        {
            var id = new FileId(fileId);
            FileStore
                .GetDescriptor(id)
                .Returns(new FsFileDescriptor(id,pathToFile));
        }

        protected void ConfigureFileDownload(string fileId, string pathToFile, Action<string> action = null)
        {
            if(action == null)
                action = s => { };

            FileStore
                .Download(new FileId(fileId), Arg.Do( action ))
                .Returns(info =>
                {
                    string tmpFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(pathToFile));
                    if(File.Exists(tmpFileName))
                        File.Delete(tmpFileName);

                    File.Copy(pathToFile, tmpFileName);
                    return tmpFileName;
                });
        }

        protected void ExpectFileUpload(string fileId, Action<string> action = null)
        {
            if(action == null)
                action = s => { };

            var id = new FileId(fileId);
            FileStore
                .Upload(id, Arg.Do( action ));

        }

        protected void CaptureCommand(Action<ICommand> action)
        {
            CommandBus.Send(Arg.Do(action));
        }
    }
}
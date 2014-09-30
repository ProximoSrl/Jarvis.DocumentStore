using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
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
    }

    [TestFixture]
    public class CreateThumbnailFromPdfJobTests : AbstractJobTest
    {
        [Test]
        public void test()
        {
            var fileStore = NSubstitute.Substitute.For<IFileStore>();
            fileStore.GetDescriptor(new FileId("doc"))
                .Returns(new FsFileStoreHandle(TestConfig.PathToDocumentPdf));

            var job = new CreateThumbnailFromPdfJob(fileStore)
            {
                Logger = new ConsoleLogger()
            };

            job.Execute(BuildContext(job, new Dictionary<string, object>{
                {JobKeys.FileId, "doc"},
                {JobKeys.FileExtension, "png"}
            }));
        }
    }
}

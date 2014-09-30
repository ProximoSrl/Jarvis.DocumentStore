using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture(Category = "jobs")]
    public class CreateThumbnailFromPdfJobTests : AbstractJobTest
    {
        [Test]
        public void should_convert_pdf_to_first_page_thumbnail()
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

            fileStore.Received().Upload(new FileId("doc.page.1.png"), Arg.Any<string>(), Arg.Any<Stream>());
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
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
            ConfigureGetFile("doc", TestConfig.PathToDocumentPdf);

            var job = BuildJob<CreateThumbnailFromPdfJob>();
            
            job.Execute(BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.FileId, "doc"},
                {JobKeys.FileExtension, "png"}
            }));

            FileStore.Received().Upload(Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>());
        }
    }
}

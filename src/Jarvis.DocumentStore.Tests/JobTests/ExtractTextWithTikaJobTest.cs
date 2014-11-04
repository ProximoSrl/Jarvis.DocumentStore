using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.Document.DocumentFormat;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture(Category = "jobs")]
    public class ExtractTextWithTikaJobTest : AbstractJobTest
    {
        [Test]
        public void should_extract_html_from_pdf()
        {
            ConfigureFileDownload("pdf", TestConfig.PathToDocumentPdf);
            SetupCreateNew(new BlobId(DocumentFormats.Content,1));

            var job = BuildJob<ExtractTextWithTikaJob>();

            job.Execute(BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.BlobId, "pdf"}
            }));

            BlobStore.Received().Upload(Arg.Any<DocumentFormat>(), Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>());
        }
    }
}
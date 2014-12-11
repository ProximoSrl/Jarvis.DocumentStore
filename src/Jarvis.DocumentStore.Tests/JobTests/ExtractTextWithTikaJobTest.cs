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
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Shared.Serialization;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.Document.DocumentFormat;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    public abstract class AbstractTikaJobTest : AbstractJobTest
    {
        [Test]
        public void should_extract_html_from_pdf()
        {
            ConfigureFileDownload("pdf", TestConfig.PathToDocumentPdf);
            SetupCreateNew(new BlobId(DocumentFormats.Content,1));

            var job = BuildTikaJob();

            job.Execute(AbstractJobTest.BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.BlobId, "pdf"}
            }));

            BlobStore.Received().Upload(Arg.Any<DocumentFormat>(), Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>());
        }

        [Test]
        public void should_extract_html_from_doc()
        {
            ConfigureFileDownload("doc", TestConfig.PathToMultipageWordDocument);
            var stream = SetupCreateNew(new BlobId(DocumentFormats.Content,1));

            var job = BuildTikaJob();

            job.Execute(AbstractJobTest.BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.BlobId, "doc"}
            }));

            BlobStore.Received().Upload(Arg.Any<DocumentFormat>(), Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>());

            // test documentcontent (skips UTF-8 BOM)
            var json = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
            var content = JsonConvert.DeserializeObject<DocumentContent>(json, PocoSerializationSettings.Default);
            Assert.NotNull(content);
            Assert.AreEqual(1, content.Pages.Length);
            Assert.IsTrue(content.Metadata.Any(x=>x.Name == DocumentContent.MetadataWithoutPageInfo));
        }

        protected abstract AbstractTikaJob BuildTikaJob();
    }

    [TestFixture(Category = "jobs")]
    public class ExtractTextWithTikaJobTest : AbstractTikaJobTest
    {
        protected override AbstractTikaJob BuildTikaJob()
        {
            return BuildJob<ExtractTextWithTikaJob>();
        }
    }

    [TestFixture(Category = "jobs")]
    public class ExtractTextWithTikaNetJobTest : AbstractTikaJobTest
    {
        protected override AbstractTikaJob BuildTikaJob()
        {
            return BuildJob<ExtractTextWithTikaJob>();
        }
    }
}
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
            var content = RunJob(TestConfig.PathToMultipageWordDocument);

            Assert.NotNull(content);
            Assert.AreEqual(1, content.Pages.Length);
            Assert.AreEqual(1, content.Pages[0].PageNumber);
            Assert.IsTrue(content.Metadata.Any(x=>x.Name == DocumentContent.MetadataWithoutPageInfo));
        }

        private DocumentContent RunJob(string pathToFile)
        {
            ConfigureFileDownload("doc", pathToFile);
            var stream = SetupCreateNew(new BlobId(DocumentFormats.Content, 1));

            var job = BuildTikaJob();

            job.Execute(AbstractJobTest.BuildContext(job, new Dictionary<string, object>
            {
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.BlobId, "doc"}
            }));

            BlobStore.Received().Upload(Arg.Any<DocumentFormat>(), Arg.Any<FileNameWithExtension>(), Arg.Any<Stream>());

            // test documentcontent (skips UTF-8 BOM)
            var json = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
            var content = JsonConvert.DeserializeObject<DocumentContent>(json, PocoSerializationSettings.Default);
            return content;
        }

        [Test]
        public void document_should_be_italian()
        {
            var content = RunJob(TestConfig.PathToLangFile("it"));

            Assert.NotNull(content);
            Assert.AreEqual(1, content.Pages.Length);
            Assert.AreEqual(1, content.Pages[0].PageNumber);
            var language = content.SafeGetMetadata(DocumentContent.MedatataLanguage);
            Assert.AreEqual("ita", language);
        }

        [Test]
        public void multipage_docx_language_should_be_italian()
        {
            var content = RunJob(TestConfig.PathToMultilanguageDocx);

            Assert.NotNull(content);
            Assert.AreEqual(1, content.Pages.Length);   // DOCX is single page content
            Assert.AreEqual(1, content.Pages[0].PageNumber);
            var language = content.SafeGetMetadata(DocumentContent.MedatataLanguage);
            Assert.AreEqual("ita", language);
        }

        [Test]
        public void multipage_pdf_language_should_be_english()
        {
            var content = RunJob(TestConfig.PathToMultilanguagePdf);

            Assert.NotNull(content);
            Assert.AreEqual(2, content.Pages.Length);   // PDF has pages info
            Assert.AreEqual(1, content.Pages[0].PageNumber);
            Assert.AreEqual(2, content.Pages[1].PageNumber);
            var language = content.SafeGetMetadata(DocumentContent.MedatataLanguage);
            Assert.AreEqual("eng", language);
        }

        [Test]
        public void image_should_extract_not_null_content_without_text()
        {
            var content = RunJob(TestConfig.PathToMediumJpg);

            Assert.NotNull(content);
            Assert.AreEqual(0, content.Pages.Length);   //No paged
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
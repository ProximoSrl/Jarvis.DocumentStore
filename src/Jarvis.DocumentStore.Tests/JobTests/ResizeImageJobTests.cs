using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture(Category = "jobs")]
    public class ResizeImageJobTests : AbstractJobTest
    {
        [Test]
        public void should_resize_image()
        {
            IList<FileNameWithExtension> files = new List<FileNameWithExtension>();
            FileStore.CreateNew(Arg.Any<FileNameWithExtension>()).Returns(info =>
            {
                files.Add(info.Arg<FileNameWithExtension>());
                var fileWriter = new FileStoreWriter(new FileId("sample_1"), new MemoryStream(), new FileNameWithExtension("a.file"));
                return fileWriter;
            });

            ConfigureGetFile("1", TestConfig.PathToDocumentPng);

            var job = BuildJob<ImageResizeJob>();
            job.ConfigService = new ConfigService();

            job.Execute(BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.FileId, "1"},
                {JobKeys.FileExtension, "png"},
                {JobKeys.Sizes, "small|large"}
            }));

            Assert.AreEqual(2, files.Count);
            Assert.AreEqual("1.small", files[0].FileName);
            Assert.AreEqual("1.large", files[1].FileName);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture]
    public class AnalyzeEmailJobTests : AbstractJobTest
    {
        [Test]
        public void should_upload_zip_file_and_send_new_format_command()
        {
            // arrange

            string storedFileName = null;
            ICommand command = null;

            ConfigureFileDownload("file_1", TestConfig.PathToEml);
            ExpectFileUpload(f => storedFileName = f);
            CaptureCommand(c => command = c);

            var job = BuildJob<AnalyzeEmailJob>();
            
            // act
            job.Execute(AbstractJobTest.BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.BlobId, "File_1"}
            }));

            // assert
            BlobStore.Received(1).Upload(Arg.Any<DocumentFormat>(), Arg.Any<string>());
            Assert.NotNull(storedFileName);
            Assert.IsTrue(storedFileName.EndsWith("message.ezip"));

            Assert.IsNotNull(command);
            Assert.IsAssignableFrom<AddFormatToDocument>(command);
        }
    }
}

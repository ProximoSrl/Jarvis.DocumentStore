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
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    public class LibreOfficeToPdfJobTests : AbstractJobTest
    {
        [Test]
        public void file_that_contains_link_to_other_file()
        {
            var blobId = new BlobId(DocumentFormats.Content, 1);
            ConfigureFileDownload(blobId, TestConfig.PowerpointWithLinkedDocuments);
           

            SetupCreateNew(blobId);
            var cService = new ConfigService();
            var converter = new LibreOfficeUnoConversion(cService);
            converter.Logger = new CQRS.TestHelpers.TestLogger();
            var job = new LibreOfficeToPdfJob(converter);
            job.ConfigService = cService;
            job.Logger = new CQRS.TestHelpers.TestLogger();
            job.BlobStore = BlobStore;
            
            job.Execute(AbstractJobTest.BuildContext(job, new Dictionary<string, object>{
                {JobKeys.TenantId, TestConfig.Tenant},
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.BlobId, (String) blobId}
            }));


        }

       
    }

   
}
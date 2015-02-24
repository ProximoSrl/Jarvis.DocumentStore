using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Attachments
{
    public class AttachmentOutOfProcessJob  : AbstractOutOfProcessPollerJob
    {

        public AttachmentOutOfProcessJob()
        {
            base.PipelineId = "attachments";
            base.QueueName = "attachments";
        }

        protected async override Task<bool> OnPolling(Shared.Jobs.PollerJobParameters parameters, string workingFolder)
        {
            string localFile = await DownloadBlob(
                 parameters.TenantId,
                 parameters.JobId,
                 parameters.FileName,
                 workingFolder);

            var extension = Path.GetExtension(localFile);
            if (extension == ".zip") 
            {
                //we can handle unzipping everything.
                var unzippingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                ZipFile.ExtractToDirectory(localFile, unzippingDirectory);
                Int32 attachCounter = 1;
                foreach (string file in Directory.EnumerateFiles(unzippingDirectory, "*.*", SearchOption.AllDirectories))
                {
                    var relativeFileName = file.Substring(unzippingDirectory.Length);
                    await AddAttachmentToHandle(
                        parameters.TenantId,
                        parameters.JobId,
                        file,
                        "attachments",
                        new DocumentHandle("attachment" + attachCounter++),
                        null);
                }
            }
            return true;
        }
    }
}

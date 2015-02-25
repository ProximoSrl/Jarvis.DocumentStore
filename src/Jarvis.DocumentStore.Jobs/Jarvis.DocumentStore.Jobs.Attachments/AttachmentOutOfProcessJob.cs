using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using MsgReader;
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
             var unzippingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (extension == ".zip") 
            {
                //we can handle unzipping everything.
               
                ZipFile.ExtractToDirectory(localFile, unzippingDirectory);
                foreach (string file in Directory.EnumerateFiles(unzippingDirectory, "*.*", SearchOption.AllDirectories))
                {
                    var relativeFileName = file.Substring(unzippingDirectory.Length);
                    await AddAttachmentToHandle(
                        parameters.TenantId,
                        parameters.JobId,
                        file,
                        "attachment_zip",
                        new Dictionary<string, object>()
                        {
                            {"RelativePath", relativeFileName}   
                        });
                }
            }
            if (extension == ".eml" || extension == ".msg") 
            {
                var reader = new Reader();
                if (!Directory.Exists(unzippingDirectory)) Directory.CreateDirectory(unzippingDirectory);
                reader.ExtractToFolder(localFile, unzippingDirectory);
      
                foreach (string file in Directory.EnumerateFiles(unzippingDirectory, "*.*", SearchOption.AllDirectories))
                {
                    if ((Path.GetExtension(file) == ".htm" || Path.GetExtension(file) == ".html") && 
                        Path.GetFileName(file).StartsWith(Path.GetFileName(parameters.FileName)))
                        continue;

                    var relativeFileName = file.Substring(unzippingDirectory.Length);
                    await AddAttachmentToHandle(
                        parameters.TenantId,
                        parameters.JobId,
                        file,
                        "attachment_email",
                        new Dictionary<string, object>()
                        {
                            {"RelativePath", relativeFileName}   
                        });
                }

            }
            return true;
        }
    }
}

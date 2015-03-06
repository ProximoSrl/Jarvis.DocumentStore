using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using MsgReader;
using MsgReader.Outlook;
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
            var unzippingDirectory = Path.Combine(workingFolder, Guid.NewGuid().ToString());
            if (!Directory.Exists(unzippingDirectory)) Directory.CreateDirectory(unzippingDirectory);
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
                        "content_zip",
                        new Dictionary<string, object>()
                        {
                            {JobsConstants.AttachmentRelativePath, relativeFileName}   
                        });
                }
            }
            else if (extension == ".eml") 
            {
                var reader = new Reader();
                reader.ExtractToFolder(localFile, unzippingDirectory);
      
                foreach (string file in Directory.EnumerateFiles(unzippingDirectory, "*.*", SearchOption.AllDirectories))
                {
                    if ((Path.GetExtension(file) == ".htm" || Path.GetExtension(file) == ".html") && 
                        Path.GetFileNameWithoutExtension(file).StartsWith(Path.GetFileNameWithoutExtension(parameters.FileName)))
                        continue;

                    if (Path.GetExtension(file) == ".htm")
                        continue;

                    var relativeFileName = file.Substring(unzippingDirectory.Length);
                    if (Logger.IsDebugEnabled) 
                    {
                        Logger.DebugFormat("Found attachment for file {0} - file {1}",
                            Path.GetFileName(localFile), relativeFileName);
                    }
                    await AddAttachmentToHandle(
                        parameters.TenantId,
                        parameters.JobId,
                        file,
                        "attachment_email",
                        new Dictionary<string, object>()
                        {
                            {JobsConstants.AttachmentRelativePath, relativeFileName}   
                        });
                }

            }
            else if (extension == ".msg")
            {
                using (var stream = File.Open(localFile, FileMode.Open, FileAccess.Read))
                using (var message = new Storage.Message(stream)) 
                {
                    foreach (Storage.Attachment attachment in message.Attachments.OfType < Storage.Attachment>())
                    {
                        if (attachment.IsInline)
                            continue; //no need to uncompress inline attqach

                        String fileName = Path.Combine(unzippingDirectory, attachment.FileName);
                        File.WriteAllBytes(fileName, attachment.Data);

                        await AddAttachmentToHandle(
                            parameters.TenantId,
                            parameters.JobId,
                            fileName,
                            "attachment_email",
                            new Dictionary<string, object>()
                        {
                            {JobsConstants.AttachmentRelativePath, attachment.FileName}   
                        });
                    }
                }
            }
           

            return true;
        }
    }
}

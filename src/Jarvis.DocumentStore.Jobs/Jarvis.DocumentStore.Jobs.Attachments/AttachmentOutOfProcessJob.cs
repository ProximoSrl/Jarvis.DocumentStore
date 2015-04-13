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

            String[] permittedExtension = null;
            if (parameters.All.ContainsKey("extensions"))
            {
                var extensionsPermitted = parameters.All["extensions"];
                if (extensionsPermitted != "*")
                {
                    permittedExtension = extensionsPermitted.Split('|');
                }
            }

            var extension = Path.GetExtension(localFile);
            var unzippingDirectory = Path.Combine(workingFolder, Guid.NewGuid().ToString());
            if (!Directory.Exists(unzippingDirectory)) Directory.CreateDirectory(unzippingDirectory);
            if (extension == ".zip") 
            {
                //we can handle unzipping everything.
               
                ZipFile.ExtractToDirectory(localFile, unzippingDirectory);
                foreach (string file in Directory.EnumerateFiles(unzippingDirectory, "*.*", SearchOption.AllDirectories))
                {
                    var attachmentExtension = Path.GetExtension(file).Trim('.');
                    if (permittedExtension != null &&
                        !permittedExtension.Contains(attachmentExtension, StringComparer.OrdinalIgnoreCase))
                    {
                        Logger.DebugFormat("job: {0} File {1} attachment is discharded because extension {2} is not permitted",
                            parameters.JobId, file, attachmentExtension);
                        continue;
                        ;
                    }
                    var relativeFileName = file.Substring(unzippingDirectory.Length);
                    await AddAttachmentToHandle(
                        parameters.TenantId,
                        parameters.JobId,
                        file,
                        "content_zip",
                        relativeFileName,
                        new Dictionary<string, object>() {}
                        );
                }
            }
            else if (extension == ".eml") 
            {
                using (var stream = File.Open(localFile, FileMode.Open, FileAccess.Read))
                {
                    var message = MsgReader.Mime.Message.Load(stream);
                    var bodyPart = message.HtmlBody ?? message.TextBody;
                    String body = "";
                    if (bodyPart != null) body = bodyPart.GetBodyAsText();
                    foreach (MsgReader.Mime.MessagePart attachment in message.Attachments.OfType<MsgReader.Mime.MessagePart>())
                    {
                        if (!String.IsNullOrEmpty(attachment.ContentId) &&
                            body.Contains(attachment.ContentId)) 
                        {
                            if (Logger.IsDebugEnabled) 
                            {
                                Logger.DebugFormat("Attachment cid {0} name {1} discharded because it is inline", attachment.ContentId, attachment.FileName);
                                continue;
                            }
                        }

                        String fileName = Path.Combine(unzippingDirectory, attachment.FileName);
                        File.WriteAllBytes(fileName, attachment.Body);
                        await AddAttachmentToHandle(
                            parameters.TenantId,
                            parameters.JobId,
                            fileName,
                            "attachment_email",
                            attachment.FileName,
                        new Dictionary<string, object>() {}
                        );
                    }
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
                           attachment.FileName,
                            new Dictionary<string, object>() { }
                        );
                    }
                }
            }
           

            return true;
        }
    }
}

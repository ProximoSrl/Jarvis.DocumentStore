using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using SolidEdgeCommunity.Reader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Jobs.SolidEdge
{
    public class SolidEdgeThumbnailGeneratorJob : AbstractOutOfProcessPollerJob
    {
        public SolidEdgeThumbnailGeneratorJob()
        {
            base.PipelineId = "sethumb";
            base.QueueName = "sethumb";
        }

        protected async override Task<ProcessResult> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            String format = parameters.All.GetOrDefault(JobKeys.ThumbnailFormat)?.ToLower() ?? "png";
            ImageFormat imageFormat;
            switch (format)
            {
                case "png":
                    imageFormat = ImageFormat.Png;
                    break;
                case "bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;
                default:
                    imageFormat = ImageFormat.Png;
                    break;
            }
            Logger.DebugFormat("Conversion for jobId {0} in format {1} starting", parameters.JobId, format);
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder).ConfigureAwait(false);
            using (SolidEdgeDocument document = SolidEdgeDocument.Open(pathToFile))
            {
                Logger.Debug(String.Format("ClassId: '{0}'", document.ClassId));
                Logger.Debug(String.Format("CreatedVersion: '{0}'", document.CreatedVersion));
                Logger.Debug(String.Format("LastSavedVersion: '{0}'", document.LastSavedVersion));
                Logger.Debug(String.Format("Created: '{0}'", document.Created));
                Logger.Debug(String.Format("LastModified: '{0}'", document.LastModified));
                Logger.Debug(String.Format("Status: '{0}'", document.Status));

                String thumbFileName = Path.ChangeExtension(pathToFile, "." + format);
                using (Bitmap bitmap = document.GetThumbnail())
                {
                    bitmap.Save(thumbFileName, imageFormat);
                }
                if (File.Exists(thumbFileName))
                {
                    await AddFormatToDocumentFromFile(
                        parameters.TenantId,
                        parameters.JobId,
                        new DocumentFormat(DocumentFormats.RasterImage),
                        thumbFileName,
                        new Dictionary<string, object>());

                    Logger.DebugFormat("Conversion of {0} in format {1} done", parameters.JobId, format);
                    return ProcessResult.Ok;
                }
            }

            return ProcessResult.Fail("Unable to extract thumbnail");
        }
    }
}

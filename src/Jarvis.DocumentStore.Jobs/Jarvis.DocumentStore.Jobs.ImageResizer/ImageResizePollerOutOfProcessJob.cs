using System.Collections.Generic;
using System.IO;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using System;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Drawing.Imaging;

namespace Jarvis.DocumentStore.Jobs.ImageResizer
{
    public class ImageResizePollerOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        public ImageResizePollerOutOfProcessJob()
        {
            base.PipelineId = "img";
            base.QueueName = "imgResize";
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            var fileExtension = parameters.All[JobKeys.ThumbnailFormat];
            ImageFormat format = GetFormatFromExtension(fileExtension);
            var sizesAsString = parameters.All[JobKeys.Sizes];
            var imageSizes = SizeInfoHelper.Deserialize(sizesAsString);

            Logger.DebugFormat("Starting resize job for {0} - {1}", parameters.JobId, sizesAsString);

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder);


            using (var sourceStream = File.OpenRead(pathToFile))
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        Logger.DebugFormat("Resize job for {0} - {1}", parameters.JobId, size.Name);
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var fileFormat = new Client.Model.DocumentFormat("thumb." + size.Name);

                        string resizeImagePath = Path.Combine(
                            workingFolder, 
                            String.Format("{0}.{1}.{2}" ,Path.GetFileNameWithoutExtension(parameters.FileName),size.Name , fileExtension));
                        resizeImagePath = SanitizeFileNameForLength(resizeImagePath);
                        using (var outStream = File.OpenWrite(resizeImagePath))
                        {
                            Logger.DebugFormat("Resizing {0}", parameters.JobId);
                            ImageResizer.Shrink(pageStream, outStream, size.Width, size.Height, format);
                        }
                        await AddFormatToDocumentFromFile(
                            parameters.TenantId, 
                            parameters.JobId, 
                            fileFormat,
                            resizeImagePath, new Dictionary<string, object>());
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", parameters.JobId, sizesAsString);
            return true;
        }

        private ImageFormat GetFormatFromExtension(string fileExtension)
        {
            if ("png".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Png;
            else if ("tiff".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Tiff;
            else if ("jpeg".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Jpeg;
            else if ("jpg".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Jpeg;
            else if ("bmp".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Bmp;
            else if ("emf".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Emf;
            else if ("wmf".Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                return ImageFormat.Wmf;
            Logger.WarnFormat("Configuration error for Image Resizer, format {0} is not supported, default to png", fileExtension);

            return ImageFormat.Png;
        }
    }
}

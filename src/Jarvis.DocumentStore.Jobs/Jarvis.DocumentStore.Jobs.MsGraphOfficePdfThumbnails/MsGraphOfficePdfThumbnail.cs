using Castle.Core.Logging;
using Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>
    public class MsGraphOfficePdfThumbnail
    {
        private readonly AuthenticationService _authenticationService;
        private readonly OfficeToPdfConverterOptions _officeToPdfConverterOptions;
        private readonly ILogger _logger;

        public MsGraphOfficePdfThumbnail(
            AuthenticationService authenticationService,
            OfficeToPdfConverterOptions officeToPdfConverterOptions,
            ILogger logger)
        {
            _authenticationService = authenticationService;
            _officeToPdfConverterOptions = officeToPdfConverterOptions;
            _logger = logger;
        }

        public async Task<string> CreateThumbnail(
            string fileFullPath,
            string outputDirectory)
        {
            //Three step conversion, first of all upload the document
            _logger.InfoFormat("About to convert file {0}", Path.GetFileName(fileFullPath));

            using (var fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var path = $"{_officeToPdfConverterOptions.GraphEndpoint}sites/{_officeToPdfConverterOptions.SiteId}/drive/items/";
                var fsc = new FileService(_authenticationService);
                var fileId = await fsc.UploadStreamAsync(path, fs, fileFullPath).ConfigureAwait(false);

                // now I've create a file in remote sharepoint, we need to delete
                byte[] converted;
                try
                {
                    converted = await fsc.DownloadFileThumbnailAsync(path, fileId).ConfigureAwait(false);
                    var convertedFile = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(fileFullPath) + ".png");
                    File.WriteAllBytes(convertedFile, converted);
                    _logger.InfoFormat("Converted {0} to {1}", Path.GetFileName(fileFullPath), convertedFile);

                    return  convertedFile;
                }
                finally
                {
                    await fsc.DeleteFileAsync(path, fileId).ConfigureAwait(false);
                }
            }
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}

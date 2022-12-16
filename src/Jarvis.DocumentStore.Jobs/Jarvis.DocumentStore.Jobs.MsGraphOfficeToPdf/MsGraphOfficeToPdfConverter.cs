using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficeToPdf
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>
    public class MsGraphOfficeToPdfConverter
    {
        private readonly AuthenticationService _authenticationService;
        private readonly OfficeToPdfConverterOptions _officeToPdfConverterOptions;
        private readonly HashSet<string> _permittedExtensions;
        private readonly ILogger _logger;

        public MsGraphOfficeToPdfConverter(
            AuthenticationService authenticationService,
            OfficeToPdfConverterOptions officeToPdfConverterOptions,
            ILogger logger)
        {
            _permittedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var extension in "xls|xlsx|xlsm|xlsb|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp".Split('|'))
            {
                _permittedExtensions.Add(extension);
            }
            _authenticationService = authenticationService;
            _officeToPdfConverterOptions = officeToPdfConverterOptions;
            _logger = logger;
        }

        public async Task<string[]> ConvertFileAsync(
            string fileFullPath,
            string outputDirectory)
        {
            //Three step conversion, first of all upload the document
            _logger.InfoFormat("About to convert file {0}", Path.GetFileName(fileFullPath));

            if (UnpermittedExtension(fileFullPath))
            {
                //we do not have anything to convert
                return Array.Empty<string>();
            }

            using (var fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var path = $"{_officeToPdfConverterOptions.GraphEndpoint}sites/{_officeToPdfConverterOptions.SiteId}/drive/items/";
                var fsc = new FileService(_authenticationService);
                var fileId = await fsc.UploadStreamAsync(path, fs, fileFullPath).ConfigureAwait(false);

                // now I've create a file in remote sharepoint, we need to delete
                byte[] converted;
                try
                {
                    converted = await fsc.DownloadConvertedFileAsync(path, fileId, "pdf").ConfigureAwait(false);
                    var convertedFile = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(fileFullPath) + ".pdf");
                    File.WriteAllBytes(convertedFile, converted);
                    _logger.InfoFormat("Converted {0} to {1}", Path.GetFileName(fileFullPath), convertedFile);
                    return new[] { convertedFile };
                }
                finally
                {
                    await fsc.DeleteFileAsync(path, fileId).ConfigureAwait(false);
                }
            }
        }

        private bool UnpermittedExtension(string fileFullPath)
        {
            return !_permittedExtensions.Contains(Path.GetExtension(fileFullPath).Trim('.'));
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}

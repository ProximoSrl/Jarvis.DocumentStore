﻿using System;
using System.Drawing.Printing;
using System.IO.Compression;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using TuesPechkin;
using System.Linq;

using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Jobs.HtmlZip
{
    /// <summary>
    /// Version that does not need blob store.
    /// </summary>
    public class HtmlToPdfConverterFromDiskFile
    {
        const bool ProduceOutline = false;
        private readonly String _inputFileName;
        public ILogger Logger { get; set; }
        readonly JobsHostConfiguration _config;

        private readonly String[] unzippedHtmlExtension = new[] { ".html", ".htm" };

        public HtmlToPdfConverterFromDiskFile(String inputFileName, JobsHostConfiguration config)
        {
            _inputFileName = inputFileName;
            _config = config;
        }

        private Boolean IsUnzippedHtmlFile()
        {
            var fileExtension = Path.GetExtension(_inputFileName);
            return unzippedHtmlExtension.Any(s => s.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Elaborate
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public String Run(String tenantId, String jobId)
        {
            Logger.DebugFormat("Converting {0} to pdf", jobId);
            var localFileName = DownloadLocalCopy(tenantId, jobId);
            var outputFileName = localFileName + ".pdf";
            var uri = new Uri(localFileName);

            var document = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    ProduceOutline = ProduceOutline,
                    PaperSize = PaperKind.A4, // Implicit conversion to PechkinPaperSize
                    Margins =
                    {
                        All = 1.375,
                        Unit = Unit.Centimeters
                    },
                    OutputFormat = GlobalSettings.DocumentOutputFormat.PDF
                },
                Objects = {
                    new ObjectSettings
                    {
                        PageUrl = uri.AbsoluteUri,
                        WebSettings = new WebSettings()
                        {
                            EnableJavascript = false,
                            PrintMediaType = false
                        }
                    },
                }
            };
            //This is the thread safe converter
            //it seems sometimes to hang forever during tests.
            //https://github.com/tuespetre/TuesPechkin
            //IConverter converter =
            //     new ThreadSafeConverter(
            //         new PdfToolset(
            //             new Win32EmbeddedDeployment(
            //                 new TempFolderDeployment())));

            //Standard, non thread safe converter.
            IConverter converter =
                new StandardConverter(
                    new PdfToolset(
                        new Win32EmbeddedDeployment(
                            new TempFolderDeployment())));

            var pdf = converter.Convert(document);

            File.WriteAllBytes(outputFileName, pdf);

            Logger.DebugFormat("Deleting {0}", localFileName);
            File.Delete(localFileName);
            Logger.DebugFormat("Conversion of {0} to pdf done!", jobId);

            return outputFileName;
        }

        string DownloadLocalCopy(String tenantId, String jobId)
        {
            Logger.DebugFormat("Downloaded {0}", _inputFileName);

            if (IsUnzippedHtmlFile()) return _inputFileName;

            var workingFolder = Path.GetDirectoryName(_inputFileName);
            ZipFile.ExtractToDirectory(_inputFileName, workingFolder);
            Logger.DebugFormat("Extracted zip to {0}", workingFolder);

            var htmlFile = Path.ChangeExtension(_inputFileName, "html");
            if (File.Exists(htmlFile))
            {
                Logger.DebugFormat("Html file is {0}", htmlFile);
                return htmlFile;
            }

            htmlFile = Path.ChangeExtension(_inputFileName, "htm");
            if (File.Exists(htmlFile))
            {
                Logger.DebugFormat("Html file is {0}", htmlFile);
                return htmlFile;
            }

            var msg = string.Format("Html file not found for {0}!", jobId);
            Logger.Error(msg);
            throw new Exception(msg);
        }
    }
}

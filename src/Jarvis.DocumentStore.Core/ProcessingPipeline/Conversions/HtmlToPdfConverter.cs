using System;
using System.Drawing.Printing;
using System.IO;
using System.IO.Compression;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using TuesPechkin;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions
{
    public class HtmlToPdfConverter
    {
        const bool ProduceOutline = false;
        readonly IFileStore _fileStore;
        public ILogger Logger { get; set; }
        readonly ConfigService _config;

        public HtmlToPdfConverter(IFileStore fileStore, ConfigService config)
        {
            _fileStore = fileStore;
            _config = config;
        }

        public FileId Run(FileId fileId)
        {
            Logger.DebugFormat("Converting {0} to pdf", fileId);
            var localFileName = DownloadLocalCopy(fileId);
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

            var converter = Factory.Create();
            var pdf = converter.Convert(document);
            var pdfFileId = new FileId(fileId + ".pdf");

            using (var source = new MemoryStream(pdf))
            {
                _fileStore.Upload(
                    pdfFileId, 
                    new FileNameWithExtension(Path.ChangeExtension(Path.GetFileName(uri.LocalPath), "pdf")),
                    source
                );
            }

            Logger.DebugFormat("Deleting {0}", localFileName);
            File.Delete(localFileName);
            Logger.DebugFormat("Conversion of {0} to pdf done!", fileId);

            return pdfFileId;
        }

        string DownloadLocalCopy(FileId fileId)
        {
            var folder = _config.GetWorkingFolder(fileId);
            if(Directory.Exists(folder))
                Directory.Delete(folder,true);

            var localFileName = _fileStore.Download(fileId, folder);
            Logger.DebugFormat("Downloaded {0}", localFileName);

            var workingFolder = Path.GetDirectoryName(localFileName);
            ZipFile.ExtractToDirectory(localFileName, workingFolder);
            Logger.DebugFormat("Extracted zip to {0}", workingFolder);

            var htmlFile = Path.ChangeExtension(localFileName, "html");
            if (File.Exists(htmlFile))
            {
                Logger.DebugFormat("Html file is {0}", htmlFile);
                return htmlFile;
            }
            
            htmlFile = Path.ChangeExtension(localFileName, "htm");
            if (File.Exists(htmlFile))
            {
                Logger.DebugFormat("Html file is {0}", htmlFile);
                return htmlFile;
            }

            var msg = string.Format("Html file not found for {0}!", fileId);
            Logger.Error(msg);
            throw new Exception(msg);
        }
    }
}

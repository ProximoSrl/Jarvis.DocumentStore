using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Storage;
using TuesPechkin;

namespace Jarvis.ImageService.Core.ProcessingPipeline.Conversions
{
    public class ConvertHtmlToPdfTask
    {
        const bool ProduceOutline = false;
        readonly IFileStore _fileStore;
        public ILogger Logger { get; set; }
        public ConvertHtmlToPdfTask(IFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public void ConvertToPdf(string fileId, Uri uri)
        {
            Logger.DebugFormat("Converting to pdf {0}", uri);
            if (!uri.IsFile)
            {
                throw new Exception(string.Format("Not a file: {0}", uri));
            }
            
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
            using (var source = new MemoryStream(pdf))
            {
                _fileStore.Upload(
                    fileId, 
                    Path.GetFileName(uri.LocalPath),
                    source
                );
            }
        }
    }
}

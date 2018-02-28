using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Pdf;

namespace Jarvis.DocumentStore.Jobs.PdfConverter.Converters
{
    public class TextPdfConverter : PdfConverterBase, IPdfConverter
    {
        public bool CanConvert(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return extension.EndsWith("txt", StringComparison.OrdinalIgnoreCase) ||
                extension.EndsWith("text", StringComparison.OrdinalIgnoreCase) ||
                extension.EndsWith("log", StringComparison.OrdinalIgnoreCase);
        }

        const bool unicode = false;
        const PdfSharp.Pdf.PdfFontEmbedding embedding = PdfSharp.Pdf.PdfFontEmbedding.Always;

        public Boolean Convert(String inputFileName, String outputFileName)
        {
            try
            {
                var text = File.ReadAllText(inputFileName);

                // Create a MigraDoc document
                Document document = new Document();
                var section = document.AddSection();
                var paragraph = section.AddParagraph();
                paragraph.Format.Font.Size = 12;
                paragraph.AddFormattedText(text);

                PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(unicode, embedding);
                pdfRenderer.Document = document;
                pdfRenderer.RenderDocument();
                pdfRenderer.PdfDocument.Save(outputFileName);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WarnFormat(ex, "Error converting file {0} to Pdf.", inputFileName);
                return false;
            }
        }
    }
}

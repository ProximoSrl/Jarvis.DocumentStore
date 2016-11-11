using Castle.Core.Logging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.PdfComposer
{
    public class PdfManipulator : IDisposable
    {
        PdfDocument _pdfDocument;
        private readonly ILogger _logger;

        public PdfManipulator(String fileName, ILogger logger) : this(logger)
        {
            _pdfDocument = PdfReader.Open(fileName, PdfDocumentOpenMode.Modify);
        }

        public PdfManipulator(ILogger logger)
        {
            _pdfDocument = new PdfDocument();
            _logger = logger;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _pdfDocument.Dispose();
            }
        }

        public PdfManipulator Save(String outputFileName)
        {
            _pdfDocument.Save(outputFileName);
            return this;
        }

        public PdfManipulator Save(Stream outputStream)
        {
            _pdfDocument.Save(outputStream);
            return this;
        }


        //http://www.pdfsharp.com/PDFsharp/index.php?option=com_content&task=view&id=52&Itemid=60
        /// <summary>
        /// Code taken from the above link.
        /// </summary>
        /// <param name="pdfDocumentToAppend"></param>
        /// <returns></returns>
        public String AppendDocumentAtEnd(String pdfDocumentToAppend)
        {
            try
            {
                // Open the document to import pages from it.
                PdfDocument inputDocument = PdfReader.Open(pdfDocumentToAppend, PdfDocumentOpenMode.Import);

                // Iterate pages
                int count = inputDocument.PageCount;
                for (int idx = 0; idx < count; idx++)
                {
                    // Get the page from the external document...
                    PdfPage page = inputDocument.Pages[idx];
                    // ...and add it to the output document.
                    _pdfDocument.AddPage(page);
                }
                return "";
            }
            catch (Exception ex)
            {
                _logger.WarnFormat(ex, "Error appending pdf {0}", pdfDocumentToAppend);
                return ex.Message;
            }
        }

        public PdfManipulator AddPageNumber()
        {
            // Make a font and a brush to draw the page counter.
            XFont font = new XFont("Verdana", 8);
            XBrush brush = XBrushes.Black;
            string noPages = _pdfDocument.Pages.Count.ToString();
            for (int i = 0; i < _pdfDocument.Pages.Count; ++i)
            {
                PdfPage page = _pdfDocument.Pages[i];

                // Make a layout rectangle.
                XRect layoutRectangle = new XRect(0/*X*/, page.Height - font.Height - 6/*Y*/, page.Width/*Width*/, font.Height/*Height*/);

                XGraphics gfx = XGraphics.FromPdfPage(page);
                gfx.DrawString(
                    "Page " + (i + 1).ToString() + " of " + noPages,
                    font,
                    brush,
                    layoutRectangle,
                    XStringFormats.Center);
            }
            return this;
        }

    }
}

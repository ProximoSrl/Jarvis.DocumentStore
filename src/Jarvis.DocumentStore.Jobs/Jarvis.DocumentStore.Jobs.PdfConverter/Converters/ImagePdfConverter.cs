using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace Jarvis.DocumentStore.Jobs.PdfConverter.Converters
{
    public class ImagePdfConverter : PdfConverterBase, IPdfConverter
    {
        private String[] supportedImageExtensions =
            new string[] { "bmp", "jpeg", "jpg", "gif", "png", "tiff", "tif" };


        public bool CanConvert(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return supportedImageExtensions.Any(e => extension.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        }

        public bool Convert(string inputFileName, string outputFileName)
        {
            try
            {
                using (PdfDocument doc = new PdfDocument())
                {
                    PdfPage page = doc.AddPage();
                    using (XGraphics gfx = XGraphics.FromPdfPage(page))
                    using (var image = XImage.FromFile(inputFileName))
                    {
                        var border = 10;
                        var widthRatio = (gfx.PageSize.Width - border * 2) / image.PixelWidth;
                        var heightRatio = (gfx.PageSize.Height - border) / image.PixelHeight;
                        var scaling = Math.Min(widthRatio, heightRatio);
                        gfx.DrawImage(image, border, border, image.PixelWidth * scaling, image.PixelHeight * scaling);
                        doc.Save(outputFileName);
                    }
                }
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

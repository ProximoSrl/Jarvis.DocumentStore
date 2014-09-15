using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using GraphicsMagick;

namespace Jarvis.ImageService.Core.ProcessinPipeline
{
    public class CreatePdfImageTask
    {
        private static readonly IEnumerable<MagickFormat> Formats;
        ILogger _logger = NullLogger.Instance;

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        static CreatePdfImageTask()
        {
            Formats = Enum.GetValues(typeof(MagickFormat)).Cast<MagickFormat>();
        }

        private string BuildOutFileName(string pattern, string originalFileName, int page)
        {
            var folder = Path.GetDirectoryName(originalFileName);
            var ext = Path.GetExtension(originalFileName) ?? string.Empty;
            if (ext.Length > 0 && ext[0] == '.')
            {
                ext = ext.Remove(0, 1);
            }
            var name = Path.GetFileNameWithoutExtension(originalFileName);

            var outFileName = pattern
                .Replace("{FOLDER}", folder)
                .Replace("{NAME}", name)
                .Replace("{EXT}", ext)
                .Replace("{PAGE}", page.ToString());

            return outFileName;
        }

        public void Convert(ConvertParams convertParams, Action<string, Stream> writer)
        {
            var settings = new MagickReadSettings
            {
                Density = new MagickGeometry(convertParams.Dpi, convertParams.Dpi)
            };

            var imageExtension = Path.GetExtension(convertParams.DestFileNamePattern).ToLowerInvariant().Remove(0, 1);
            MagickFormat imageFormat = convertParams.Format
                ?? Formats.First(x => string.Compare(x.ToString(), imageExtension, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0);

            Logger.DebugFormat("File extension is {0}", imageExtension);
            Logger.DebugFormat("Image format is {0}", imageFormat.ToString());

            using (var images = new MagickImageCollection())
            {
                images.Read(convertParams.SourceStream, settings);

                var lastImage = Math.Min(convertParams.FromPage - 1 + convertParams.Pages, images.Count) - 1;

                for (int page = convertParams.FromPage - 1; page <= lastImage; page++)
                {
                    var fname = BuildOutFileName(convertParams.DestFileNamePattern, convertParams.SourceFilename, page + 1);
                    Logger.DebugFormat("Preview file is {0}", fname);
                    var image = images[page];

                    image.Format = imageFormat;
                    using (var ms = new MemoryStream())
                    {
                        image.Write(ms);
                        ms.Seek(0L, SeekOrigin.Begin);
                        writer(fname, ms);
                    }
                }
            }
        }
    }
}

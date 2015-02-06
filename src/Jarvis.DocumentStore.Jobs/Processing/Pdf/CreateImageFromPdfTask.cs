using System;
using System.IO;
using Castle.Core.Logging;
using GraphicsMagick;

namespace Jarvis.DocumentStore.Jobs.Processing.Pdf
{
    public class CreateImageFromPdfTask
    {
        private static readonly object LockForInitializationIssue = new object();
        private static bool _firstDone = false;

        ILogger _logger = NullLogger.Instance;
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public void Run(Stream sourceStream, CreatePdfImageTaskParams createPdfImageTaskParams, Action<int, Stream> pageWriter)
        {
            var settings = new MagickReadSettings
            {
                Density = new MagickGeometry(createPdfImageTaskParams.Dpi, createPdfImageTaskParams.Dpi)
            };

            MagickFormat imageFormat = TranslateFormat(createPdfImageTaskParams.Format);

            Logger.DebugFormat("Image format is {0}", imageFormat.ToString());
            using (var images = new MagickImageCollection())
            {
                bool done = false;
                if (_firstDone == false)
                {
                    lock (LockForInitializationIssue)
                    {
                        if (_firstDone == false)
                        {
                            images.Read(sourceStream, settings);
                            done = true;
//                            _firstDone = true;
                        }
                    }
                }

                if (!done)
                    images.Read(sourceStream, settings);

                var lastImage = Math.Min(createPdfImageTaskParams.FromPage - 1 + createPdfImageTaskParams.Pages, images.Count) - 1;
                for (int page = createPdfImageTaskParams.FromPage - 1; page <= lastImage; page++)
                {
                    var image = images[page];
                    image.Format = imageFormat;
                    
                    using (var ms = new MemoryStream())
                    {
                        image.Write(ms);
                        ms.Seek(0L, SeekOrigin.Begin);
                        pageWriter(page + 1, ms);
                    }
                }
            }
        }

        MagickFormat TranslateFormat(CreatePdfImageTaskParams.ImageFormat format)
        {
            switch (format)
            {
                case CreatePdfImageTaskParams.ImageFormat.Png:
                    return MagickFormat.Png;

                case CreatePdfImageTaskParams.ImageFormat.Jpg:
                    return MagickFormat.Jpg;
                
                default:
                    throw new ArgumentOutOfRangeException("format");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Castle.Core.Logging;
using ImageMagick;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

namespace Jarvis.DocumentStore.Jobs.PdfThumbnails
{
    public class CreateImageFromPdfTask
    {
        private static readonly object LockForInitializationIssue = new object();
        private static bool _firstDone;

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public PdfDecrypt Decryptor { get; set; }

        public CreateImageFromPdfTask()
        {
            Passwords = new List<string>();
        }

        /// <summary>
        /// List of known password that can be used to decrypt
        /// the pdf file. If this list is different from Empty the 
        /// component will try to decrypt the pdf before generating
        /// the images.
        /// </summary>
        public IList<String> Passwords { get; private set; }

        public async Task<Boolean> Run(
            String pathToFile,
            CreatePdfImageTaskParams createPdfImageTaskParams,
            Func<int, Stream, Task<Boolean>> pageWriter)
        {
            String tempFileName = null;
            if (Passwords.Count > 0)
            {
                tempFileName =
                    Path.Combine(Path.GetDirectoryName(pathToFile),
                    Path.GetFileNameWithoutExtension(pathToFile) + "_decrypted.pdf");
                if (Decryptor.DecryptFile(pathToFile, tempFileName, Passwords))
                {
                    pathToFile = tempFileName;
                }
            }
            using (var sourceStream = File.OpenRead(pathToFile))
            {
                var settings = new MagickReadSettings
                {
                    Density = new PointD(createPdfImageTaskParams.Dpi, createPdfImageTaskParams.Dpi)
                };
                settings.FrameIndex = 0; // First page
                settings.FrameCount = 1; // Number of pages
                MagickFormat imageFormat = TranslateFormat(createPdfImageTaskParams.Format);

                Logger.DebugFormat("Image format is {0}", imageFormat.ToString());
                using (var images = new MagickImageCollection())
                {
                    bool done = false;
                    if (!_firstDone)
                    {
                        lock (LockForInitializationIssue)
                        {
                            if (!_firstDone)
                            {
                                images.Read(sourceStream, settings);
                                done = true;
                            }
                        }
                    }

                    if (!done)
                        images.Read(sourceStream, settings);

                    var lastImage =
                        Math.Min(createPdfImageTaskParams.FromPage - 1 + createPdfImageTaskParams.Pages, images.Count) -
                        1;
                    for (int page = createPdfImageTaskParams.FromPage - 1; page <= lastImage; page++)
                    {
                        var image = images[page];
                        image.Format = imageFormat;

                        using (var ms = new MemoryStream())
                        {
                            image.Write(ms);
                            ms.Seek(0L, SeekOrigin.Begin);
                            await pageWriter(page + 1, ms).ConfigureAwait(false);
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(tempFileName) && File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }
            return true;
        }

        private MagickFormat TranslateFormat(CreatePdfImageTaskParams.ImageFormat format)
        {
            switch (format)
            {
                case CreatePdfImageTaskParams.ImageFormat.Png:
                    return MagickFormat.Png;

                case CreatePdfImageTaskParams.ImageFormat.Jpg:
                    return MagickFormat.Jpg;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }
}

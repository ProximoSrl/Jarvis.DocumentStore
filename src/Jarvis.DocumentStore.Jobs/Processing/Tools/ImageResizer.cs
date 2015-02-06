using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Jarvis.DocumentStore.Jobs.Processing.Tools
{
    public static class ImageResizer
    {
        public static void Shrink(Stream source, Stream destination, int maxWidth, int maxHeight)
        {
            using (var image = Image.FromStream(source))
            {
                var ratioX = (double)maxWidth / image.Width;
                var ratioY = (double)maxHeight / image.Height;
                var ratio = Math.Min(ratioX, ratioY);
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);

                var newImage = new Bitmap(newWidth, newHeight);

                using (var g = Graphics.FromImage(newImage))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                newImage.Save(destination, ImageFormat.Png);
            }
        }
    }
}

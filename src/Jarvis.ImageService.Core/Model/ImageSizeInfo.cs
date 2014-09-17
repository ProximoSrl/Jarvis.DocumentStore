using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Jarvis.ImageService.Core.Model
{
    public class ImageSizeInfo
    {
        public string Name { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ImageSizeInfo(string name, int width, int height)
        {
            Name = name.ToLowerInvariant();
            Width = width;
            Height = height;
        }
    }
}

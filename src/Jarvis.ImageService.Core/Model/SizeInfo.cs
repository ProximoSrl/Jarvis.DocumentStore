using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ImageService.Core.Model
{
    public static class SizeInfoHelper
    {
        public static string Serialize(IEnumerable<SizeInfo> sizes)
        {
            var sb = new StringBuilder();
            foreach (var sizeInfo in sizes)
            {
                if (sb.Length > 0)
                    sb.Append("|");

                sb.AppendFormat(
                    "{0}:{1}x{2}",
                    sizeInfo.Name,
                    sizeInfo.Width,
                    sizeInfo.Height
                );
            }

            return sb.ToString();
        }
    }

    public class SizeInfo
    {
        public string Name { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public SizeInfo(string name, int width, int height)
        {
            Name = name.ToLowerInvariant();
            Width = width;
            Height = height;
        }
    }
}

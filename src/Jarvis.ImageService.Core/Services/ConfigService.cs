using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;

namespace Jarvis.ImageService.Core.Services
{
    public class ConfigService
    {
        private ImageSizeInfo[] ImageSizes { get; set; }

        public ConfigService()
        {
            this.ImageSizes = new ImageSizeInfo[]
            {
                new ImageSizeInfo("small", 200, 200),
                new ImageSizeInfo("large", 800, 800)
            };
        }

        public ImageSizeInfo[] GetDefaultSizes()
        {
            return this.ImageSizes;
        }
    }
}

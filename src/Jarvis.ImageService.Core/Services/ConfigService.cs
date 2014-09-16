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
        private SizeInfo[] Sizes { get; set; }

        public ConfigService()
        {
            this.Sizes = new SizeInfo[]
            {
                new SizeInfo("small", 200, 200),
                new SizeInfo("large", 800, 800)
            };
        }

        public SizeInfo[] GetDefaultSizes()
        {
            return this.Sizes;
        }
    }
}

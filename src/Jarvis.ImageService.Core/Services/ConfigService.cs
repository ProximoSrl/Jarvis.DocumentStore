using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

        public string GetPathToLibreOffice()
        {
            return GetConfigValue("LIBREOFFICE_PATH");
        }

        public string GetWorkingFolder(string fileId)
        {
            return EnsureFolder(Path.Combine(GetConfigValue("TEMP"), fileId));
        }

        string GetConfigValue(string key)
        {
            return  ConfigurationManager.AppSettings[key] ??
                    Environment.GetEnvironmentVariable(key);
        }

        string EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }
    }
}

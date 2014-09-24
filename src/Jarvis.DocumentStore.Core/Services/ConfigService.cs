using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Services
{
    public class ConfigService
    {
        readonly ImageSizeInfo[] _imageSizes;
        readonly string[] _allowedExtensions;
        
        public ConfigService()
        {
            _imageSizes = SizeInfoHelper.Deserialize(GetConfigValue(
                "JARVIS_IMGSRVCS_SIZES",
                "small:200x200|large:800x800"
            ).ToLowerInvariant());

            _allowedExtensions = GetConfigValue(
                "JARVIS_IMGSRVCS_ALLOWED_FILE_TYPES",
                ".pdf|.xls|.xlsx|.docx|.doc|.ppt|.pptx|.pps|.ppsx|.rtf|.odt|.ods|.odp|.htmlzip"
            ).ToLowerInvariant().Split('|');
        }

        public ImageSizeInfo[] GetDefaultSizes()
        {
            return this._imageSizes;
        }

        public string GetPathToLibreOffice()
        {
            return GetConfigValue("LIBREOFFICE_PATH");
        }

        public string GetWorkingFolder(string fileId)
        {
            return EnsureFolder(Path.Combine(GetConfigValue("TEMP"), fileId));
        }

        string GetConfigValue(string key, string defaultValue = null)
        {
            return  ConfigurationManager.AppSettings[key] ??
                    Environment.GetEnvironmentVariable(key) ?? 
                    defaultValue;
        }

        string EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }

        public bool IsFileAllowed(string filename)
        {
            var ext = Path.GetExtension(filename.Replace("\"", "")).ToLowerInvariant();
            return _allowedExtensions.Contains(ext);
        }
    }
}

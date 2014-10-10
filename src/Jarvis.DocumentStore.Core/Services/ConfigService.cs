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

            var configExtensions = GetConfigValue(
                "JARVIS_IMGSRVCS_ALLOWED_FILE_TYPES",
                "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|htmlzip|eml|msg|jpeg|jpg|png"
                ).ToLowerInvariant();

            _allowedExtensions = configExtensions != "*" ? configExtensions.Split('|') : null;

            IsDeduplicationActive = GetConfigValue(
                "JARVIS_DOCUMENTSTORE_DEDUPLICATION",
                "on"
            ).ToLowerInvariant() == "on";
        }

        public bool IsDeduplicationActive { get; private set; }

        public ImageSizeInfo[] GetDefaultSizes()
        {
            return this._imageSizes;
        }

        public string GetPathToLibreOffice()
        {
            return GetConfigValue("LIBREOFFICE_PATH");
        }

        public string GetPathToJava()
        {
            var javaHome = GetConfigValue("JAVA_HOME");
            if (String.IsNullOrEmpty(javaHome))
                throw new Exception("Please set JAVA_HOME in app.config or env variable");

            var pathToJavaExe = Path.Combine(javaHome, "bin\\java.exe");
            if (!File.Exists(pathToJavaExe))
            {
                throw new Exception(string.Format("Java not found on {0}", pathToJavaExe));
            }

            return pathToJavaExe;
        }

        public string GetPathToTika()
        {
            var pathToTika = GetConfigValue("TIKA_HOME");
            if (!File.Exists(pathToTika))
            {
                throw new Exception(string.Format("Tika not found on {0}", pathToTika));
            }

            return pathToTika;
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

        public bool IsFileAllowed(FileNameWithExtension filename)
        {
            if (_allowedExtensions == null)
                return true;

            return _allowedExtensions.Contains(filename.Extension);
        }
    }
}

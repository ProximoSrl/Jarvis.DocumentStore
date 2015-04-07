using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;


namespace Jarvis.DocumentStore.JobsHost.Support
{
    public class JobsHostConfiguration
    {
        public int QueueJobsPollInterval { get; protected set; }

        public bool UseEmbeddedTika { get; private set; }

        public JobsHostConfiguration()
        {
            UseEmbeddedTika = GetConfigValue(
               "JARVIS_DOCUMENTSTORE_TIKA_EMBEDDED",
               "true"
           ).ToLowerInvariant() == "true";

            QueueJobsPollInterval = Int32.Parse(GetConfigValue("queues.jobs-poll-interval-ms", "1000"));
        }

        public string GetPathToLibreOffice()
        {
            var libreOffice = GetConfigValue("LIBREOFFICE_PATH");
            if(String.IsNullOrWhiteSpace(libreOffice))
                throw new Exception("Please set LIBREOFFICE_PATH in app.config or env variable");

            return libreOffice;
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
        
        public string GetWorkingFolder(string tenantId, string blobId)
        {
            if (tenantId == null) throw new ArgumentNullException("tenantId");
            if (blobId == null) throw new ArgumentNullException("blobId");
            return EnsureFolder(Path.Combine(GetConfigValue("TEMP"), tenantId, blobId));
        }

        public string GetWorkingFolderForQueue(string tenantId, string queueName)
        {
            if (tenantId == null) throw new ArgumentNullException("tenantId");
            if (queueName == null) throw new ArgumentNullException("queueName");
            return EnsureFolder(Path.Combine(GetConfigValue("TEMP"), tenantId, queueName));
        }

        string GetConfigValue(string key, string defaultValue = null)
        {
            return ConfigurationManager.AppSettings[key] ??
                    Environment.GetEnvironmentVariable(key) ??
                    defaultValue;
        }

        string EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }

        public virtual void CreateLoggingFacility(LoggingFacility f)
        {
            //Check log4net.config location, we can accept a local log4net.config
            //or a general one located in parent folder of the job
            var parentLog4net = new FileInfo("..\\log4net.config");
            if (parentLog4net.Exists) 
            {
                f.LogUsing(new ExtendedLog4netFactory(parentLog4net.FullName));
            }
            else
            {
                f.LogUsing(new ExtendedLog4netFactory("log4net.config"));
            }
            
        }
    }
}

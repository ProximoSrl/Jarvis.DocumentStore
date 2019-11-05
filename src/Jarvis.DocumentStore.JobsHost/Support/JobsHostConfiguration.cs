using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using log4net.Repository.Hierarchy;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Core;

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

			QueueJobsPollInterval = Int32.Parse(GetConfigValue("queues.jobs-poll-interval-ms", "2000"));
		}

		public string GetPathToLibreOffice()
		{
			var libreOffice = GetConfigValue("LIBREOFFICE_PATH");
			if (String.IsNullOrWhiteSpace(libreOffice))
				throw new Exception("Please set LIBREOFFICE_PATH in app.config or env variable");

			//Lets trim " char, because we do not need it.
			return libreOffice.Trim('\"');
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

		public string GetWorkingFolder(string tenantId, string blobId)
		{
			if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));
			if (blobId == null) throw new ArgumentNullException(nameof(blobId));
			return EnsureFolder(Path.Combine(GetConfigValue("TEMP"), tenantId, blobId));
		}

		public string GetWorkingFolderForQueue(string tenantId, string queueName)
		{
			if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));
			if (queueName == null) throw new ArgumentNullException(nameof(queueName));
			return EnsureFolder(Path.Combine(GetConfigValue("TEMP"), tenantId, queueName));
		}

		public string GetConfigValue(string key, string defaultValue = null)
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
			Console.WriteLine("START: Searching log4net in: {0}", parentLog4net.FullName);
			if (parentLog4net.Exists)
			{
				f.LogUsing(new ExtendedLog4netFactory(parentLog4net.FullName));
			}
			else
			{
				Console.WriteLine("FAILED: Searching log4net in: {0}", parentLog4net.FullName);
				var log4net = new FileInfo("log4net.config");
				Console.WriteLine("Use Default log4net in: {0}", log4net.FullName);
				if (!log4net.Exists)
				{
					Console.Error.WriteLine("ERROR, UNABLE TO FIND LOG4NET IN: {0}", log4net.FullName);
                    Hierarchy hierarchy = (Hierarchy) LogManager.GetRepository();

                    RollingFileAppender roller = new RollingFileAppender();
                    PatternLayout layout = new PatternLayout("% date{ MMM / dd / yyyy HH:mm: ss,fff} [%thread] %-5level %logger %ndc – %message%newline");
                    roller.AppendToFile = false;
                    roller.File = @"Logs\error.txt";
                    roller.Layout = layout;
                    roller.MaxSizeRollBackups = 5;
                    roller.MaximumFileSize = "1GB";
                    roller.RollingStyle = RollingFileAppender.RollingMode.Size;
                    roller.StaticLogFileName = true;
                    roller.ActivateOptions();
                    roller.Threshold = Level.Warn;
                    hierarchy.Root.AddAppender(roller);

                    ConsoleAppender ca = CreateConsoleAppender();
                    hierarchy.Root.AddAppender(ca);
                    hierarchy.Root.Level = Level.Info;
                    hierarchy.Configured = true;
                }
				f.LogUsing(new ExtendedLog4netFactory(log4net.FullName));
			}
		}

        private static ConsoleAppender CreateConsoleAppender()
        {
            ConsoleAppender appender = new ConsoleAppender();
            appender.Name = "ConsoleAppender";
            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "% date{ MMM / dd / yyyy HH:mm: ss,fff} [%thread] %-5level %logger %ndc – %message%newline";
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }
    }
}

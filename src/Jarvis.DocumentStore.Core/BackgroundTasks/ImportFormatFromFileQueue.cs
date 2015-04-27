using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Shared.Serialization;
using Jarvis.Framework.Shared.MultitenantSupport;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.BackgroundTasks
{
    public class FileInQueue
    {
        public Uri Uri { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public DocumentFormat Format { get; private set; }
        public TenantId Tenant { get; private set; }
        public DocumentCustomData CustomData { get; private set; }

        public string PathToTaskFile { get; set; }
    }

    public class ImportFormatFromFileQueue
    {
        public const string JobExtension = "*.dsimport";
        public ILogger Logger { get; set; }

        private readonly string[] _foldersToWatch;
        public ImportFormatFromFileQueue(string[] foldersToWatch)
        {
            _foldersToWatch = foldersToWatch;

        }

        public void PollFileSystem()
        {
            foreach (var folder in _foldersToWatch)
            {
                var files = Directory.GetFiles(folder, JobExtension, SearchOption.AllDirectories);
                Parallel.ForEach(files, file =>
                {
                    var task = LoadTask(file);
                    if (task != null)
                    {
                        Logger.DebugFormat("Loading /{0}/{1}/{2} - {3}", 
                            task.Tenant,
                            task.Handle,
                            task.Format,
                            task.Uri
                        );
                    
                    }
                });
            }
        }

        public FileInQueue LoadTask(string pathToFile)
        {
            try
            {
                var asJson = File.ReadAllText(pathToFile)
                    .Replace("%CURRENT_DIR%", Path.GetDirectoryName(pathToFile).Replace("\\", "/"));

                var task = JsonConvert.DeserializeObject<FileInQueue>(asJson, PocoSerializationSettings.Default);
                task.PathToTaskFile = pathToFile;
                return task;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "failed to deserialize {0}", pathToFile);
                return null;
            }
        }
    }
}

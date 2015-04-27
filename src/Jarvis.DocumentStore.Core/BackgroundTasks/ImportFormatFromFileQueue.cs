﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Shared.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NEventStore.Serialization;

namespace Jarvis.DocumentStore.Core.BackgroundTasks
{
    public class FileInQueue
    {
        public Uri Uri { get; private set; }
    }

    public class ImportFormatFromFileQueue
    {
        public const string JobExtension = "*.dsimport";
        public ILogger Logger { get; set; }

        private readonly string[] _foldersToWatch;
        private readonly ConcurrentDictionary<string, FileInQueue> _filesToImport;
        public ImportFormatFromFileQueue(string[] foldersToWatch)
        {
            _foldersToWatch = foldersToWatch;
            _filesToImport = new ConcurrentDictionary<string,FileInQueue>();

        }

        public void PollFileSystem()
        {
            foreach (var folder in _foldersToWatch)
            {
                var files = Directory.GetFiles(folder, JobExtension, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    _filesToImport.GetOrAdd(file.ToLowerInvariant(), LoadTask);
                }
            }
        }

        public FileInQueue LoadTask(string pathToFile)
        {
            try
            {
                var asJson = File.ReadAllText(pathToFile)
                    .Replace("%CURRENT_DIR%", Path.GetDirectoryName(pathToFile).Replace("\\", "/"));

                var task = JsonConvert.DeserializeObject<FileInQueue>(asJson, PocoSerializationSettings.Default);
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

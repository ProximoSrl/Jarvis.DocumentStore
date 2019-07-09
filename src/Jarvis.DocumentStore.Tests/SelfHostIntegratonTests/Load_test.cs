using System.Linq;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Threading;
using System.IO;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture, Explicit]
    [Category("Explicit")]
    public class Load_test
    {
        private DocumentStoreServiceClient _docs;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _docs = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.DocsTenant
            );
        }

        [Test]
        public void Drop_all_tenant()
        {
            MongoDbTestConnectionProvider.DropAll();
            if (Directory.Exists(@"Z:\temp\docsstorage"))
                Directory.Delete(@"Z:\temp\docsstorage", true);
        }

        [Test]
        public void Upload_TempDirectory()
        {
            var files = Directory.GetFiles(@"X:\temp\testupload", "*.*", SearchOption.AllDirectories);
            Parallel.ForEach(files, file =>
            {
                {
                    if (!Path.GetFileName(file).StartsWith("."))
                    {
                        _docs.UploadAsync(file, DocumentHandle.FromString(SanitizeFileName(file))).Wait();
                    }
                }
            });
        }

        private static string SanitizeFileName(string file)
        {
            return file.Replace("/", "_").Replace("\\", "_").Replace("#", "_");
        }

        [Test]
        public void Upload_TempDirectory_FLOOD()
        {
            var files = Directory.GetFiles(@"X:\temp\testupload", "*.*", SearchOption.AllDirectories);
            List<Task> taskList = new List<Task>();
            foreach (var file in files.Where(f => !Path.GetFileName(f).StartsWith(".")))
            {
                //NO WAIT. This will FLOOD documentstore
                var task = _docs.UploadAsync(file, DocumentHandle.FromString(SanitizeFileName(file)));
                taskList.Add(task);
                if (taskList.Count > 100)
                {
                    Task.WaitAll(taskList.ToArray());
                    taskList.Clear();
                }
            }
        }
    }
}
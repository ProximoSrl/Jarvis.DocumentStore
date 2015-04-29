﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ClientTests
{
    [TestFixture]
    public class DocumentStoreClientTests
    {
        private static readonly DocumentHandle  Doc = new DocumentHandle("doc");
        private static readonly Guid TaskId = new Guid("9a29d730-f57a-41e4-92ba-55b7d99712a2");
        [Test]
        public void should_create_a_valid_document_import_data()
        {
            var client = new DocumentStoreServiceClient(new Uri("http://ds"), "test");

            var did = client.CreateDocumentImportData(TaskId, "c:\\temp\\a file.docx", Doc);

            Assert.AreEqual("file:///c:/temp/a%20file.docx",did.Uri.AbsoluteUri);
            Assert.AreEqual("test", did.Tenant);
            Assert.AreEqual(Doc, did.Handle);
            Assert.IsFalse(did.DeleteAfterImport);
            Assert.AreEqual(DocumentStoreServiceClient.OriginalFormat, did.Format);
        }

        [Test]
        public void should_serialize_document_import_data()
        {
            var fname = Path.Combine(TestConfig.TempFolder, "a_file_to_import");
            var client = new DocumentStoreServiceClient(new Uri("http://ds"), "test");
            var did = client.CreateDocumentImportData(TaskId,"c:\\temp\\a file.docx", Doc);
            client.QueueDocumentImport(did, fname);
            
            Assert.IsTrue(File.Exists(fname+".dsimport"));

            const string expected = 
@"{
  ""TaskId"": ""9a29d730-f57a-41e4-92ba-55b7d99712a2"",
  ""Uri"": ""c:\\temp\\a file.docx"",
  ""Handle"": ""doc"",
  ""Format"": ""original"",
  ""Tenant"": ""test"",
  ""CustomData"": null,
  ""DeleteAfterImport"": false
}";

            Assert.AreEqual(expected, File.ReadAllText(fname + ".dsimport"));
        }

        [Test, Explicit]
        public void queue_folder()
        {
            var sourceFolder = @"c:\Downloads\queue\";
            var taskFolder = @"c:\temp\dsqueue";
            var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            var client = new DocumentStoreServiceClient(new Uri("http://ds"), "docs");
            var counter = 1;
            Parallel.ForEach(files, file =>
            {
                var handle = "import_" + counter++;
                var task = client.CreateDocumentImportData(
                    Guid.NewGuid(),
                    file,
                    new DocumentHandle(handle)
                );

                task.DeleteAfterImport = true;

                var dsFile = Path.Combine(taskFolder, handle);
                client.QueueDocumentImport(task, dsFile);
            });
        }
    }
}

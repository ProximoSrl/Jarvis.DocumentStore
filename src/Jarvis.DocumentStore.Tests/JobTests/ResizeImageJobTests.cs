﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NSubstitute;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    [TestFixture(Category = "jobs")]
    public class ResizeImageJobTests : AbstractJobTest
    {
        [Test]
        public void should_resize_image()
        {
            IList<FileNameWithExtension> files = new List<FileNameWithExtension>();
            FileStore.CreateNew(Arg.Any<FileId>(), Arg.Any<FileNameWithExtension>()).Returns(info =>
            {
                files.Add(info.Arg<FileNameWithExtension>());
                return new MemoryStream();
            });

            ConfigureGetFile("doc.png", TestConfig.PathToDocumentPng);

            var job = BuildJob<ImageResizeJob>();
            job.ConfigService = new ConfigService();

            job.Execute(BuildContext(job, new Dictionary<string, object>{
                {JobKeys.DocumentId, "Document_1"},
                {JobKeys.FileId, "doc.png"},
                {JobKeys.FileExtension, "png"},
                {JobKeys.Sizes, "small|large"}
            }));

            Assert.AreEqual(2, files.Count);
            Assert.AreEqual("doc.png.small", files[0].FileName);
            Assert.AreEqual("doc.png.large", files[1].FileName);
        }
    }
}
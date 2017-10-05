using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fasterflect;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Tests.JobTests.Queue
{
    [TestFixture]
    public class QueueInfoTests
    {
        [Test]
        public void Verify_basic_deserialization_of_empty_queue()
        {
            dynamic test = JsonConvert.DeserializeObject(@"[]");
            List<QueueInfo> listOfQueue = new List<QueueInfo>();
            RemoteDocumentStoreConfiguration.ParseQueueList(listOfQueue, test);
            Assert.That(listOfQueue, Has.Count.EqualTo(0));
        }

        [Test]
        public void Verify_basic_deserialization_with_only_name()
        {
            dynamic test = JsonConvert.DeserializeObject(@"[{'name' : 'tika'}]");
            List<QueueInfo> listOfQueue = new List<QueueInfo>();
            RemoteDocumentStoreConfiguration.ParseQueueList(listOfQueue, test);
            Assert.That(listOfQueue, Has.Count.EqualTo(1));
            Assert.That(listOfQueue[0].Name, Is.EqualTo("tika"));
        }

        [TestCase("html|test|xlsx", "test.html", true)]
        [TestCase("htm|test|xlsx", "test.html", false)]
		[TestCase("html|test|xlsx", "test...html", true)]
		public void Verify_should_create_job_extension(String extensions, String fileName, Boolean expected)
        {
            QueueInfo sut = new QueueInfo("TEST", extensions : extensions);
            StreamReadModel sr = new StreamReadModel();
            sr.Filename = new Core.Model.FileNameWithExtension(fileName);
            Assert.That(sut.ShouldCreateJob(sr), Is.EqualTo(expected));
        }

        [TestCase("application/zip|message/rfc822|application/vnd.ms-outlook", "test.zip", true)]
        [TestCase("application/zip", "test.html", false)]
        public void Verify_should_create_job_mimetypes(String mimetypes, String fileName, Boolean expected)
        {
            QueueInfo sut = new QueueInfo("TEST", mimeTypes : mimetypes);
            StreamReadModel sr = new StreamReadModel();
            sr.Filename = new Core.Model.FileNameWithExtension(fileName);
            Assert.That(sut.ShouldCreateJob(sr), Is.EqualTo(expected));
        }

        [TestCase("application/zip", "xlsx|html", "test.zip", true)]
        [TestCase("application/zip", "xlsx|html", "test.xlsx", true)]
        [TestCase("application/zip", "xlsx|html", "test.doc", false)]
        public void Verify_should_create_job_mime_extension(String mimetypes, String extensions, String fileName, Boolean expected)
        {
            QueueInfo sut = new QueueInfo("TEST", extensions : extensions, mimeTypes: mimetypes);
            StreamReadModel sr = new StreamReadModel();
            sr.Filename = new Core.Model.FileNameWithExtension(fileName);
            Assert.That(sut.ShouldCreateJob(sr), Is.EqualTo(expected));
        }

        [TestCase("", "", "", "test.txt")]
        public void Queue_with_only_manual_execution(
            String mimetypes, 
            String extensions,
            String pipeline, 
            String fileName)
        {
            QueueInfo sut = new QueueInfo("TEST", 
                extensions: extensions, 
                mimeTypes: mimetypes,
                pipeline : pipeline);
            StreamReadModel sr = new StreamReadModel();
            sr.Filename = new Core.Model.FileNameWithExtension(fileName);
            Assert.That(sut.ShouldCreateJob(sr), Is.EqualTo(false));
        }

        [Test]
        public void Verify_basic_deserialization_with_all_properties()
        {
            dynamic test = JsonConvert.DeserializeObject(@"[{
			    //resize image pipeline accepts every format of extension pdf
				'name' : 'imgResize',
				'pipeline' : '^(?!img$).*', //avoid recursion.
				'extensions' : 'png|jpg|gif|jpeg',
                'formats'  : 'original|tika',
				'parameters' : {
					'thumb_format' : 'png',
					'sizes' : 'small:200x200|large:800x800'
				}
			}]");
            List<QueueInfo> listOfQueue = new List<QueueInfo>();
            RemoteDocumentStoreConfiguration.ParseQueueList(listOfQueue, test);
            Assert.That(listOfQueue, Has.Count.EqualTo(1));
            Assert.That(listOfQueue[0].Name, Is.EqualTo("imgResize"));
            Assert.That(listOfQueue[0].Pipeline, Is.EqualTo("^(?!img$).*"));
            Assert.That(listOfQueue[0].Extension, Is.EqualTo("png|jpg|gif|jpeg"));
            Assert.That(listOfQueue[0].GetFieldValue("_splittedExtensions"), 
                Is.EquivalentTo(new [] {"png", "jpg", "gif", "jpeg"}));
            Assert.That(listOfQueue[0].Formats, Is.EqualTo("original|tika"));
            Assert.That(listOfQueue[0].GetFieldValue("_splittedFormats"),
                Is.EquivalentTo(new[] { "original", "tika" }));
            Assert.That(listOfQueue[0].Parameters["thumb_format"], Is.EqualTo("png"));
            Assert.That(listOfQueue[0].Parameters["sizes"], Is.EqualTo("small:200x200|large:800x800"));
        }

        [Test]
        public void Verify_job_pollers_deserialization()
        {
            dynamic test = JsonConvert.DeserializeObject(@"[{
			    //resize image pipeline accepts every format of extension pdf
				'name' : 'imgResize',
				'pipeline' : '^(?!img$).*', //avoid recursion.
				'extensions' : 'png|jpg|gif|jpeg',
                'formats'  : 'original|tika',
				'parameters' : {
					'thumb_format' : 'png',
					'sizes' : 'small:200x200|large:800x800'
				},
                'pollersInfo' : 
				[
					{ 'name' : 'OutOfProcessBaseJobManager1'},
                    { 'name' : 'OutOfProcessBaseJobManager2'}
				]
			}]");
            List<QueueInfo> listOfQueue = new List<QueueInfo>();
            RemoteDocumentStoreConfiguration.ParseQueueList(listOfQueue, test);
            Assert.That(listOfQueue, Has.Count.EqualTo(1));
            Assert.That(listOfQueue[0].PollersInfo.Length, Is.EqualTo(2));
            Assert.That(listOfQueue[0].PollersInfo[0].Name, Is.EqualTo("OutOfProcessBaseJobManager1"));
            Assert.That(listOfQueue[0].PollersInfo[1].Name, Is.EqualTo("OutOfProcessBaseJobManager2"));
        }

        [Test]
        public void Verify_job_pollers_deserialization_parameters()
        {
            dynamic test = JsonConvert.DeserializeObject(@"[{
			    //resize image pipeline accepts every format of extension pdf
				'name' : 'imgResize',
				'pipeline' : '^(?!img$).*', //avoid recursion.
				'extensions' : 'png|jpg|gif|jpeg',
                'formats'  : 'original|tika',
				'parameters' : {
					'thumb_format' : 'png',
					'sizes' : 'small:200x200|large:800x800'
				},
                'pollersInfo' : 
				[
					{ 'name' : 'OutOfProcessBaseJobManager1', 'parameters' : { 'blabla' : 'x'}},
                    { 'name' : 'OutOfProcessBaseJobManager2', 'parameters' : {'p1' : 'x1', 'p2' : 'x2'}}
				]
			}]");
            List<QueueInfo> listOfQueue = new List<QueueInfo>();
            RemoteDocumentStoreConfiguration.ParseQueueList(listOfQueue, test);
            Assert.That(listOfQueue, Has.Count.EqualTo(1));
            Assert.That(listOfQueue[0].PollersInfo.Length, Is.EqualTo(2));
            Assert.That(listOfQueue[0].PollersInfo[0].Parameters["blabla"], Is.EqualTo("x"));
            Assert.That(listOfQueue[0].PollersInfo[1].Parameters["p1"], Is.EqualTo("x1"));
            Assert.That(listOfQueue[0].PollersInfo[1].Parameters["p2"], Is.EqualTo("x2"));
        }
    }
}

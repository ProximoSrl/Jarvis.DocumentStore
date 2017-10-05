using Jarvis.DocumentStore.Jobs.Email;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.JobTests.Email
{
	[TestFixture]
	public class EmailConverterTests
	{
		[Test]
		public void Basic_email_conversion_test()
		{
			var file = TestConfig.PathToEml;
			MailMessageToHtmlConverterTask sut = new MailMessageToHtmlConverterTask();
			var converted = sut.Convert(Guid.NewGuid().ToString(), file, Path.GetTempPath());
			Assert.That(converted, Is.Not.Null);
			Console.WriteLine(converted);
		}
	}
}

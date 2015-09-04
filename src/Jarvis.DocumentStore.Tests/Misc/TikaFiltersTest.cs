using Jarvis.DocumentStore.Jobs.TikaBaseFilters;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture]
    public class TikaFiltersTest
    {

        UnnecessaryTextFileFilter utffSut = new UnnecessaryTextFileFilter();

        [Test]
        public void Verify_binary_file_should_be_analyzed()
        {
            var result = utffSut.ShouldAnalyze("attachment.msg", TestConfig.PathToMsgWithComplexAttachmentAndZipFileWithFolders);
            Assert.That(result, Is.True);
        }

        [Test]
        public void Verify_text_with_numbers_file_should_not_be_analyzed()
        {
            var result = utffSut.ShouldAnalyze("Test.jpg", TestConfig.PathToFileWithNumbers);
            Assert.That(result, Is.True);
        }
    }
}

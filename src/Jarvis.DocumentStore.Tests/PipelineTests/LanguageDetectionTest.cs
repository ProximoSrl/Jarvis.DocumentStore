using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.JobsHost.Processing.Analyzers;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class LanguageDetectionTest
    {
        [TestCase("nel mezzo del cammin di nostra vita", "ita")]
        [TestCase("il documento in oggetto rappresenta il contratto di vendita o locazione", "ita")]
        [TestCase(":it", "ita")]
        [TestCase(":en", "eng")]
        [TestCase(":de", "deu")]
        [TestCase(":es", "spa")]
        [TestCase(":ko", "kor")]
        [TestCase(":ch", "zho")]
        [TestCase(":nl", "nld")]
        [TestCase(":ru", "rus")]
        public void detect_with_language_analyzer(string text, string expectedLang)
        {
            if (text[0] == ':')
            {
                text = TestConfig.ReadLangFile(text.Substring(1));
            }

            var lang = LanguageDetector.GetLanguage(text);

            Assert.AreEqual(expectedLang, lang);
        }
    }
}

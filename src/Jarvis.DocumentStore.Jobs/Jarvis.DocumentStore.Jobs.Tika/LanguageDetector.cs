using System;
using System.IO;
using System.Linq;
using IvanAkcheurov.NTextCat.Lib;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public static class LanguageDetector
    {
        private static readonly RankedLanguageIdentifier _identifier;

        static LanguageDetector()
        {
            var factory = new RankedLanguageIdentifierFactory();
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var coreProfileFile = Path.Combine(currentDirectory, "Core14.profile.xml");
            _identifier = factory.Load(coreProfileFile);
        }

        public static string GetLanguage(string text)
        {
            var languages = _identifier.Identify(text);
            var mostCertainLanguage = languages.FirstOrDefault();
            if (mostCertainLanguage != null)
            {
                // http://en.wikipedia.org/wiki/List_of_ISO_639-3_codes
                return mostCertainLanguage.Item1.Iso639_3;
            }

            return null;
        }
    }
}

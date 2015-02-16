using System.Linq;
using IvanAkcheurov.NTextCat.Lib;

namespace Jarvis.DocumentStore.JobsHost.Processing.Analyzers
{
    public class LanguageDetector
    {
        private static readonly RankedLanguageIdentifier _identifier;

        static LanguageDetector()
        {
            var factory = new RankedLanguageIdentifierFactory();
            _identifier = factory.Load("Core14.profile.xml");
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

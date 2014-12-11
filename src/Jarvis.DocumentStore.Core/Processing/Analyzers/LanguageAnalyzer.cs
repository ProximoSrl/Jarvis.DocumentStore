using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvanAkcheurov.NTextCat.Lib;

namespace Jarvis.DocumentStore.Core.Processing.Analyzers
{
    public class LanguageAnalyzer
    {
        private static readonly RankedLanguageIdentifier _identifier;

        static LanguageAnalyzer()
        {
            var factory = new RankedLanguageIdentifierFactory();
            _identifier = factory.Load("Core14.profile.xml");
        }

        public string GetLanguage(string text)
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

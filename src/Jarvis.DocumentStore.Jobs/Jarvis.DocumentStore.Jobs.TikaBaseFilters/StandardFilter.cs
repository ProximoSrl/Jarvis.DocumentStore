using Jarvis.DocumentStore.Jobs.Tika.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.TikaBaseFilters
{
    public class StandardFilter : IFilter
    {
        public string Filter(string tikaContent)
        {
            tikaContent = Regex.Replace(tikaContent, @"[\s]{2,}", " ");
            tikaContent = Regex.Replace(tikaContent, @"[\n]{2,}", "\n");
            tikaContent = Regex.Replace(tikaContent, @"[\t]{2,}", "\t");
            tikaContent = Regex.Replace(tikaContent, @"\0+", " ");
            return tikaContent;
        }

        public bool ShouldAnalyze(string fileName, string blobFileName)
        {
            return true;
        }

        public int Order
        {
            get { return 100; }
        }
    }
}

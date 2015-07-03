using Jarvis.DocumentStore.Jobs.Tika.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.TikaBaseFilters
{
    public class DecimalRemoverFilter : IFilter
    {

        public string Filter(string tikaContent)
        {
            //replace decimal with E notation
            tikaContent = Regex.Replace(tikaContent, @"(\t|\n){1}-?[\d\.]+?E[-+]\d{1,2}", "");
            
            //replace decimal between tabs, excel table content
            tikaContent = Regex.Replace(tikaContent, @"\t[\d\.\s]+\t", "");

            return tikaContent;
        }

        public bool ShouldAnalyze(string fileName, string blobFileName)
        {
            return true;
        }

        public int Order
        {
            get { return 10; }
        }
    }
}

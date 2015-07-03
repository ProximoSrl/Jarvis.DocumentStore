using Jarvis.DocumentStore.Jobs.Tika.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.TikaBaseFilters
{
    public class UnnecessaryTextFileFilter : IFilter
    {
        public int Order
        {
            get
            {
                return 1;
            }
        }

        public string Filter(string tikaContent)
        {
            return tikaContent;
        }

        public bool ShouldAnalyze(string fileName, string blobFileName)
        {
            var fi = new FileInfo(blobFileName);
            var fileSize = fi.Length;
            //Small files can be always analyzed
            if (fileSize < 16 * 1024) return true; 

            try
            {
                using (var fileStream = File.OpenRead(blobFileName))
                using (var br = new BinaryReader(fileStream))
                {
                    var fileHead = br.ReadBytes(16 * 1024);
                    //is this some file that contains mostly numbers?
                    var stringContent = System.Text.Encoding.ASCII.GetString(fileHead, 1024, fileHead.Length - 1024);
                    
                    var digitCount = stringContent.Count(c => Char.IsDigit(c) || numberChars.Contains(c));
                    //more than 95% of the text is digit, or . or , or tabs
                    return (digitCount * 100.0 / stringContent.Length) < 95;

                    //var lines = stringContent.Split('\n', '\r')
                    //    .Skip(5);

                    //if (lines.Count == 0) return true; //no more than 5 lines, binary?
                    //var linesWithNumbers = lines.Count(IsMostlyNumbers);

                    ////more than 90% of the lines are numbers
                    //return (linesWithNumbers * 100.0 / lines.Count) < 90;
                }
            }
            catch (Exception)
            {
                //Cannot read as text, encoding error, etc, file is probably
                //binary so pleas analyze it.
                return true;
            }
            return true;
        }

        private const String numberChars = ",.+-E\t\r\n";

        //private bool IsMostlyNumbers(string line)
        //{
        //    if (string.IsNullOrEmpty(line)) return false;
        //    var numCount = line.Count(c => Char.IsDigit(c) || numberChars.Contains(c));
        //    return (numCount * 100.0 / line.Length) > 95; //90% of chars are numbers
        //}
    }
}

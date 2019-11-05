using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Tika.Filters
{
    /// <summary>
    /// Filter text extracted by tika, it is used to avoid tika to bload
    /// the database with really big text.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Filter a content of tika, it receives in input the 
        /// html format of tika.
        /// </summary>
        /// <param name="tikaContent"></param>
        /// <returns></returns>
        String Filter(String tikaContent);

        /// <summary>
        /// It return true if the file should be analyzed.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Boolean ShouldAnalyze(String fileName, String blobFileName);

        Int32 Order { get; }
    }
}

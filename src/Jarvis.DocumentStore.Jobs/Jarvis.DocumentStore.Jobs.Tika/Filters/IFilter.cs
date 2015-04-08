using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Tika.Filters
{
    /// <summary>
    /// Filter text extracted by tika.
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

        Int32 Order { get; }
    }
}

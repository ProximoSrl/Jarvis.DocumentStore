using Castle.Core.Logging;
using Jarvis.DocumentStore.Jobs.Tika.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public class ContentFilterManager
    {
        private IFilter[] _filters;

        public ILogger Logger { get; set; }

        public ContentFilterManager(IFilter[] filters)
        {
            _filters = filters ?? new IFilter[0];
            Logger = NullLogger.Instance;
        }

        public String Filter(String input) 
        {
            if (_filters == null || _filters.Length == 0) return input;
            foreach (var filter in _filters.OrderBy(f => f.Order))
            {
                input = filter.Filter(input);
            }

            return input;
        }

        internal bool ShouldAnalyze(string fileName, string pathToFile)
        {
            if (_filters == null || _filters.Length == 0) return true;

            //Check if one filter prevent analysis
            foreach (var filter in _filters.OrderBy(f => f.Order))
            {
                if (!filter.ShouldAnalyze(fileName, pathToFile))
                {
                    Logger.InfoFormat("File {0} was discharded by filter {1}", fileName, filter.GetType().Name);
                    return false;
                }
            }

            return true;
        }
    } 
}

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

        public ContentFilterManager(IFilter[] filters)
        {
            _filters = filters;
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
    }
}

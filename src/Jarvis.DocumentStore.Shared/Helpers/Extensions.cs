using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Helpers
{
    public static class Extensions
    {
        public static Value GetOrDefault<Key, Value>(this IDictionary<Key, Value> dictionary, Key key) 
        {
            Value outValue;
            if (!dictionary.TryGetValue(key, out outValue))
                return default(Value);
            return outValue;
        }
    }
}

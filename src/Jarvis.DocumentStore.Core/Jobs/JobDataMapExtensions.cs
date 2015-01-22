using Quartz;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public static class JobDataMapExtensions
    {
        public static int GetIntOrDefault(this JobDataMap datamap, string key, int defaultValue)
        {
            if (datamap.ContainsKey(key))
                return datamap.GetInt(key);
            return defaultValue;
        }

        public static int GetIntOrDefault(this IDictionary<string, string> parameters, string key, int defaultvalue) 
        {
            int outValue;
            if (parameters.ContainsKey(key) &&
                int.TryParse(parameters[key], out outValue)) return outValue;

            return defaultvalue;
        }
    }
}
using Quartz;

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
    }
}
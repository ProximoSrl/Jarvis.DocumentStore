using System;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Shared.Jobs
{
    /// <summary>
    /// All Ids of this object are plain string to minimize dependency from
    /// Jarvis.Framework. This class is supposed to be used by plugin.
    /// </summary>
    public class PollerJobParameters
    {
        public String JobId { get; set; }

        public String InputDocumentFormat { get; set; }

        public String TenantId { get; set; }

        public String FileExtension { get; set; }

        public Dictionary<String, String> All { get; set; }

        public Int32 GetIntOrDefault(string key, int defaultvalue)
        {
            int outValue;
            if (All.ContainsKey(key) &&
                int.TryParse(All[key], out outValue)) return outValue;

            return defaultvalue;
        }
    }
}
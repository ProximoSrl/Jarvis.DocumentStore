using System.Collections.Generic;
using System.Linq;

namespace Jarvis.DocumentStore.Core.Domain.Handle
{
    public class HandleCustomData : Dictionary<string, object>
    {
        public HandleCustomData()
        {

        }

        public HandleCustomData(Dictionary<string, object> original)
            : base(original)
        {

        }

        public static bool IsEquals(HandleCustomData dic1, HandleCustomData dic2)
        {
            if (dic1 == null && dic2 == null)
                return true;

            if (object.ReferenceEquals(dic1, dic2))
                return true;

            if (dic1 == null || dic2 == null)
                return false;

            return dic1.Count == dic2.Count && !dic1.Except(dic2).Any();
        }
    }
}
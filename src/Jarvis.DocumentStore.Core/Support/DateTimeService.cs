using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Support
{
    public static class DateTimeService
    {
        private static DateTime StandardNow()
        {
            return DateTime.UtcNow;
        }

        private static Func<DateTime> UtcNowGenerator = StandardNow;

        public static DateTime UtcNow
        {
            get { return UtcNowGenerator(); }
        }

        internal static IDisposable Override(Func<DateTime> overrideGenerator)
        {
            UtcNowGenerator = overrideGenerator;
            return new DsDisposableAction(() => UtcNowGenerator = StandardNow);
        }
    }
}

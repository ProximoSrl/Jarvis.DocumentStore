using System;
using System.Runtime.InteropServices;

namespace Jarvis.DocumentStore.Shared.Helpers
{
    [Flags]
    internal enum ErrorModes : uint
    {
        SYSTEM_DEFAULT = 0x0,
        SEM_FAILCRITICALERRORS = 0x0001,
        SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
        SEM_NOGPFAULTERRORBOX = 0x0002,
        SEM_NOOPENFILEERRORBOX = 0x8000
    }

    /// <summary>
    /// http://stackoverflow.com/questions/12102982/disabling-windows-error-reportingappcrash-dialog-programmatically
    /// </summary>
    public static class Native
    {
        [DllImport("kernel32.dll")]
        internal static extern ErrorModes SetErrorMode(ErrorModes mode);

        public static void DisableWindowsErrorReporting()
        {
            SetErrorMode(
                ErrorModes.SEM_NOGPFAULTERRORBOX |
                ErrorModes.SEM_NOOPENFILEERRORBOX
            );
        }
    }
}

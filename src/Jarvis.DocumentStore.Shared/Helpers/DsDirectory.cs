using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyDirectory = Delimon.Win32.IO.Directory;

namespace Jarvis.DocumentStore.Shared.Helpers
{
    public static class DsDirectory
    {
        public static bool Exists(string folder)
        {
            return MyDirectory.Exists(folder);
        }

        public static String[] GetFiles(
            string folder, 
            string jobExtension,
            SearchOption searchOption)
        {
            return MyDirectory.GetFiles(folder, jobExtension, Convert(searchOption));
        }

        private static Delimon.Win32.IO.SearchOption Convert(
            System.IO.SearchOption searchOption)
        {
            switch (searchOption)
            {
                case SearchOption.AllDirectories:
                    return Delimon.Win32.IO.SearchOption.AllDirectories;
                case SearchOption.TopDirectoryOnly:
                    return Delimon.Win32.IO.SearchOption.TopDirectoryOnly;
            }
            throw new ArgumentException();
        }
    }
}

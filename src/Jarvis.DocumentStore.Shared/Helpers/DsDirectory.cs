using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if DisablePriLongPath
using MyDirectory = System.IO.Directory;
#else
using MyDirectory = Pri.LongPath.Directory;
#endif
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
            return MyDirectory.GetFiles(folder, jobExtension, searchOption);
        }

        public static void Delete(string path, bool recursive)
        {
            MyDirectory.Delete(path, recursive);
        }

        public static void CreateDirectory(string path)
        {
            MyDirectory.CreateDirectory(path);
        }

        public static IEnumerable<string> GetFiles(string dir)
        {
            return MyDirectory.GetFiles(dir);
        }

    }
}

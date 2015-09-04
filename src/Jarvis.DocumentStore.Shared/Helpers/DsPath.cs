using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if DisableDelimon
using MyPath = System.IO.Path;
#else
using MyPath = Delimon.Win32.IO.Path;
#endif
namespace Jarvis.DocumentStore.Shared.Helpers
{
    public static class DsPath
    {

        public static string GetFileNameWithoutExtension(string fileNameWithExtension)
        {
            if (String.IsNullOrEmpty(fileNameWithExtension)) return fileNameWithExtension;

            if (!fileNameWithExtension.Contains(".")) //Delimon give exception sometimes if dot is not present
                return fileNameWithExtension;
            return MyPath.GetFileNameWithoutExtension(fileNameWithExtension);
        }

        public static string GetFileName(string pathToFile)
        {
            return MyPath.GetFileName(pathToFile);
        }

        public static string GetTempFileName()
        {
            return MyPath.GetTempFileName();
        }

        public static string ChangeExtension(string path, string extension)
        {
            return System.IO.Path.ChangeExtension(path, extension);
        }

        public static string Combine(string dir, string file)
        {
            return MyPath.Combine(dir, file);
        }

        public static string Combine(string dir1, string dir2, string file)
        {
            return MyPath.Combine(MyPath.Combine(dir1, dir2), file);
        }

        public static string GetTempPath()
        {
            return MyPath.GetTempPath();
        }

        public static string GetDirectoryName(string pathToFile)
        {
            return MyPath.GetDirectoryName(pathToFile);
        }

        public static string GetExtension(string pathToFile)
        {
            return MyPath.GetExtension(pathToFile);
        }

        public static bool HasExtension(string path)
        {
            return System.IO.Path.HasExtension(path);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if DisablePriLongPath
using MyFile = System.IO.File;
#else
using MyFile = Pri.LongPath.File;
#endif

namespace Jarvis.DocumentStore.Shared.Helpers
{
    public static class DsFile
    {
        public static void Delete(string path)
        {
            MyFile.Delete(path);
        }

        public static System.IO.FileStream OpenRead(string pathToFile)
        {
            return MyFile.OpenRead(pathToFile);
        }

        public static void WriteAllText(string fileName, string textFile)
        {
            if (MyFile.Exists(fileName))
                MyFile.Delete(fileName);
            MyFile.WriteAllText(fileName, textFile);
        }

        public static String ReadAllText(string pathToFile)
        {
            return MyFile.ReadAllText(pathToFile);
        }

        public static DateTime GetLastWriteTimeUtc(string pathToFile)
        {
            return MyFile.GetLastWriteTimeUtc(pathToFile);
        }

        public static bool Exists(string path)
        {
            //https://github.com/peteraritchie/LongPath/issues/48
            if (path != null && path.Length < 240)
                return System.IO.File.Exists(path);

            return MyFile.Exists(path);
        }

        public static FileStream Open(string path, FileMode fileMode, FileAccess fileAccess)
        {
            return MyFile.Open(path, fileMode, fileAccess);
        }

        public static FileStream Open(string path, FileMode fileMode)
        {
            return MyFile.Open(path, fileMode);
        }


        public static void WriteAllBytes(string path, byte[] data)
        {
            MyFile.WriteAllBytes(path, data);
        }

        public static FileStream OpenWrite(string path)
        {
            if (!MyFile.Exists(path))
            {
                using (MyFile.Create(path)) ;
            }
            return MyFile.OpenWrite(path);
        }

        public static FileAttributes GetAttributes(string path)
        {
            return MyFile.GetAttributes(path);
        }

        public static void SetAttributes(string path, FileAttributes attributes)
        {
            MyFile.SetAttributes(path, attributes);
        }

        public static void Copy(string source, string destination)
        {
            MyFile.Copy(source, destination, false);
        }

        public static void SetLastWriteTime(string path, DateTime utcNow)
        {
            MyFile.SetLastWriteTime(path, utcNow);
        }

    }
}

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
            if (path != null && path.Length < 240)
            {
                System.IO.File.Delete(path);
            }
            else
            {
                MyFile.Delete(path);
            }
        }

        public static System.IO.FileStream OpenRead(string pathToFile)
        {
            if (pathToFile != null && pathToFile.Length < 240)
                return System.IO.File.OpenRead(pathToFile);

            return MyFile.OpenRead(pathToFile);
        }

        public static void WriteAllText(string fileName, string textFile)
        {
            if (DsFile.Exists(fileName))
                DsFile.Delete(fileName);

            if (fileName != null && fileName.Length < 240)
            {
                System.IO.File.WriteAllText(fileName, textFile);
            }
            else
            {
                MyFile.WriteAllText(fileName, textFile);
            }
        }

        public static String ReadAllText(string pathToFile)
        {
            if (pathToFile != null && pathToFile.Length < 240)
                return System.IO.File.ReadAllText(pathToFile);
            return MyFile.ReadAllText(pathToFile);
        }

        public static DateTime GetLastWriteTimeUtc(string pathToFile)
        {
            if (pathToFile != null && pathToFile.Length < 240)
                return System.IO.File.GetLastWriteTimeUtc(pathToFile);
            return MyFile.GetLastWriteTimeUtc(pathToFile);
        }

        public static bool Exists(string path)
        {
            //https://github.com/peteraritchie/LongPath/issues/48
            if (path != null && path.Length < 240)
                return System.IO.File.Exists(path);

            return MyFile.Exists(path);
        }

        public static System.IO.FileStream Create(string path)
        {
            //https://github.com/peteraritchie/LongPath/issues/48
            if (path != null && path.Length < 240)
            {
                return System.IO.File.Create(path);
            }
            else
            {
                return MyFile.Create(path);
            }
        }

        public static FileStream Open(string path, FileMode fileMode, FileAccess fileAccess)
        {
            //https://github.com/peteraritchie/LongPath/issues/48
            if (path != null && path.Length < 240)
                return System.IO.File.Open(path, fileMode, fileAccess);

            return MyFile.Open(path, fileMode, fileAccess);
        }

        public static FileStream Open(string path, FileMode fileMode)
        {
            //https://github.com/peteraritchie/LongPath/issues/48
            if (path != null && path.Length < 240)
                return System.IO.File.Open(path, fileMode);
            return MyFile.Open(path, fileMode);
        }


        public static void WriteAllBytes(string path, byte[] data)
        {
            //https://github.com/peteraritchie/LongPath/issues/48
            if (path != null && path.Length < 240)
            {
                System.IO.File.WriteAllBytes(path, data);
            }
            else
            {
                MyFile.WriteAllBytes(path, data);
            }
        }


        public static FileStream OpenWrite(string path)
        {
            if (!DsFile.Exists(path))
            {
                using (DsFile.Create(path)) ;
            }
            if (path != null && path.Length < 240)
                return System.IO.File.OpenWrite(path);

            return MyFile.OpenWrite(path);
        }

        public static FileAttributes GetAttributes(string path)
        {
            if (path != null && path.Length < 240)
                return System.IO.File.GetAttributes(path);

            return MyFile.GetAttributes(path);
        }

        public static void SetAttributes(string path, FileAttributes attributes)
        {
            if (path != null && path.Length < 240)
            {
                System.IO.File.SetAttributes(path, attributes);
            }
            else
            {
                MyFile.SetAttributes(path, attributes);
            }
        }

        public static void Copy(string source, string destination)
        {
            MyFile.Copy(source, destination, false);
        }

        public static void SetLastWriteTime(string path, DateTime utcNow)
        {
            if (path != null && path.Length < 240)
            {
                System.IO.File.SetLastWriteTime(path, utcNow);
            }
            else
            {
                MyFile.SetLastWriteTime(path, utcNow);
            }
        }

    }
}

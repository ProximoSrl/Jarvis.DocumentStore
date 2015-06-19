using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MyFile = Delimon.Win32.IO.File;

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

        public static bool Exists(string pathToTika)
        {
            return MyFile.Exists(pathToTika);
        }

        public static FileStream Open(string path, FileMode fileMode, FileAccess fileAccess)
        {
            return MyFile.Open(path, Convert(fileMode), Convert(fileAccess));
        }

        public static void WriteAllBytes(string path, byte[] data)
        {
            MyFile.WriteAllBytes(path, data);
        }

        public static FileStream OpenWrite(string path)
        {
            return MyFile.OpenWrite(path);
        }

        public static FileAttributes GetAttributes(string path)
        {
            return Convert(MyFile.GetAttributes(path));
        }

        public static void SetAttributes(string path, FileAttributes attributes)
        {
            MyFile.SetAttributes(path, Convert(attributes));
        }

        public static void Copy(string source, string destination)
        {
            MyFile.Copy(source, destination, false);
        }

        public static void SetLastWriteTime(string path, DateTime utcNow)
        {
            MyFile.SetLastWriteTime(path, utcNow);
        }

        private static Delimon.Win32.IO.FileAccess Convert(System.IO.FileAccess access)
        {
            switch (access)
            {
                case FileAccess.Read:
                    return Delimon.Win32.IO.FileAccess.Read;
                case FileAccess.Write:
                    return Delimon.Win32.IO.FileAccess.Write;
                case FileAccess.ReadWrite:
                    return Delimon.Win32.IO.FileAccess.ReadWrite;
            }
            throw new ArgumentException();
        }


        private static Delimon.Win32.IO.FileMode Convert(System.IO.FileMode mode)
        {
            switch (mode)
            {
                case FileMode.Append:
                    return Delimon.Win32.IO.FileMode.Append;
                case FileMode.Create:
                    return Delimon.Win32.IO.FileMode.Create;
                case FileMode.CreateNew:
                    return Delimon.Win32.IO.FileMode.CreateNew;
                case FileMode.Open:
                    return Delimon.Win32.IO.FileMode.Open;
                case FileMode.OpenOrCreate:
                    return Delimon.Win32.IO.FileMode.OpenOrCreate;
                case FileMode.Truncate:
                    return Delimon.Win32.IO.FileMode.Truncate;
            }
            throw new ArgumentException();
        }

        private static Delimon.Win32.IO.FileAttributes Convert(System.IO.FileAttributes attributes)
        {
            return (Delimon.Win32.IO.FileAttributes)(Int32)attributes;
        }

        private static FileAttributes 
            Convert(Delimon.Win32.IO.FileAttributes attributes)
        {
            return (FileAttributes)(Int32)attributes;
            //switch (attributes)
            //{
            //    case Delimon.Win32.IO.FileAttributes.Archive:
            //        return Delimon.Win32.IO.FileAttributes.Archive;
            //    case FileAttributes.Compressed:
            //        return Delimon.Win32.IO.FileAttributes.Compressed;
            //    case FileAttributes.Device:
            //        return Delimon.Win32.IO.FileAttributes.Device;
            //    case FileAttributes.Directory:
            //        return Delimon.Win32.IO.FileAttributes.Directory;
            //    case FileAttributes.Encrypted:
            //        return Delimon.Win32.IO.FileAttributes.Encrypted;
            //    case FileAttributes.Hidden:
            //        return Delimon.Win32.IO.FileAttributes.Hidden;
            //    case FileAttributes.Normal:
            //        return Delimon.Win32.IO.FileAttributes.Normal;
            //    case FileAttributes.NotContentIndexed:
            //        return Delimon.Win32.IO.FileAttributes.NotContentIndexed;
            //    case FileAttributes.Offline:
            //        return Delimon.Win32.IO.FileAttributes.Offline;
            //    case FileAttributes.ReadOnly:
            //        return Delimon.Win32.IO.FileAttributes.ReadOnly;
            //    case FileAttributes.ReparsePoint:
            //        return Delimon.Win32.IO.FileAttributes.ReparsePoint;
            //    case FileAttributes.SparseFile:
            //        return Delimon.Win32.IO.FileAttributes.SparseFile;
            //    case FileAttributes.System:
            //        return Delimon.Win32.IO.FileAttributes.System;
            //    case FileAttributes.Temporary:
            //        return Delimon.Win32.IO.FileAttributes.Temporary;
            //}
            //throw new ArgumentException();
        }


    }
}

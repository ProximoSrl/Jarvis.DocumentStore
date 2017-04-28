#if NETFULL

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        /// <summary>
        /// Ensure that a directory exists, and if not exists will create it.
        /// It is concurrency safe, because if some other thread/process created
        /// the very same path, the try catch ensure that no exception is thrown
        /// if the directory exists after the exception is thrown.
        /// </summary>
        /// <param name="path"></param>
        public static void EnsureDirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (MyDirectory.Exists(path))
            {
                return;
            }

            Int32 retryCount = 0;
            Exception lastException;
            do
            {
                try
                {
                    MyDirectory.CreateDirectory(path);
                    return;
                }
                catch (IOException ex)
                {
                    Thread.Sleep(1000); //could be a transient error? Wait a little bit
                    lastException = ex;
                    if (MyDirectory.Exists(path))
                    {
                        return; //directory exists, probably some other thread created it in the meanwhile
                    }
                }

            } while (retryCount++ < 5);
            //If we reach here, we were not able to create directory.
            throw new ApplicationException($"Cannot create directory {path}", lastException);
        }

        public static IEnumerable<string> GetFiles(string dir)
        {
            return MyDirectory.GetFiles(dir);
        }
    }
}
#endif
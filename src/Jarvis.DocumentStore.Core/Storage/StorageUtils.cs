using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Storage
{
    internal static class StorageUtils
    {
        /// <summary>
        /// Compute MD5 of a file
        /// </summary>
        /// <param name="pathToFile"></param>
        /// <returns></returns>
        internal static string GetMd5Hash(string pathToFile)
        {
            byte[] md5Hash;
            using (var md5 = MD5.Create())
            using (var fileStream = File.Open(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                md5Hash = md5.ComputeHash(fileStream);
            }

            return BitConverter.ToString(md5Hash).Replace("-", "");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Helpers
{
    public static class StringUtils
    {
        private static Char[] invalidChar = Path.GetInvalidFileNameChars();

        public static string ToSafeFileName(this string fileName, char replaceChar = '_')
        {
            var sb = new StringBuilder();
            foreach (var c in fileName)
            {
                if (invalidChar.Contains(c))
                    sb.Append(replaceChar);
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string ToSafeFileName(this string fileName, Int32 maxFileNameLength)
        {
            if (fileName.Length > maxFileNameLength)
            {
                fileName = fileName.Substring(0, maxFileNameLength);
            }
            return fileName.ToSafeFileName();
        }
    }
}

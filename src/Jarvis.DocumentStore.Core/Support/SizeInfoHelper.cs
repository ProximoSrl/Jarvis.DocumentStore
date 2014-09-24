using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Support
{
    public static class SizeInfoHelper
    {
        private static Regex _regex = new Regex("([a-z]+):([0-9]+)x([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Serialize(IEnumerable<ImageSizeInfo> sizes)
        {
            var sb = new StringBuilder();
            foreach (var sizeInfo in sizes)
            {
                if (sb.Length > 0)
                    sb.Append("|");

                sb.AppendFormat(
                    "{0}:{1}x{2}",
                    sizeInfo.Name,
                    sizeInfo.Width,
                    sizeInfo.Height
                    );
            }

            return sb.ToString();
        }

        public static ImageSizeInfo[] Deserialize(string sizes)
        {
            return (from s in sizes.Split('|')
                let m = _regex.Match(s)
                where m.Success
                select new ImageSizeInfo(
                    m.Groups[1].Value,
                    int.Parse(m.Groups[2].Value),
                    int.Parse(m.Groups[3].Value)
                    )).ToArray();
        }
    }
}
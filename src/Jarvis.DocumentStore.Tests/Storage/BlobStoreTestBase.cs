using Pri.LongPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests.Storage
{
    public class BlobStoreTestBase
    {
        /// <summary>
        /// Generate a temp file with some text content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string GenerateTempTextFile(String content, String fileName)
        {
            String tempFileName = Path.GetTempPath() + fileName;
            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            File.WriteAllText(tempFileName, content);
            return tempFileName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Services;
using MsgReader;

namespace Jarvis.ImageService.Core.ProcessingPipeline.Conversions
{
    /// <summary>
    /// Outlook email & msg conversion
    /// </summary>
    public class MailMessageToHtmlConverterTask
    {
        public string Convert(string pathToEml, string workingFolder)
        {
            var reader = new Reader();
            var fname = Path.GetFileNameWithoutExtension(pathToEml);
            var outFolder = Path.Combine(workingFolder, fname);

            Directory.CreateDirectory(outFolder);

            reader.ExtractToFolder(pathToEml, outFolder );

            var pathToZip = Path.Combine(workingFolder, Path.ChangeExtension(fname, "htmlzip"));
            if(File.Exists(pathToZip))
                File.Delete(pathToZip);

            ZipFile.CreateFromDirectory(outFolder, pathToZip);

            Directory.Delete(outFolder, true);

            return pathToZip;
        }
    }
}

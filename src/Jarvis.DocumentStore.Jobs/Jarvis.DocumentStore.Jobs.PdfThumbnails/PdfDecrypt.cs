using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using iTextSharp.text.pdf;
using Fasterflect;
using System.IO;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Jobs.PdfThumbnails
{
    public class PdfDecrypt
    {
        public ILogger Logger { get; set; }

        internal Boolean DecryptFile(
            string inputFile,
            string outputFile,
            IEnumerable<string> userPasswords)
        {
            foreach (var pwd in userPasswords)
            {
                try
                {
                    using (var reader = new PdfReader(inputFile, new ASCIIEncoding().GetBytes(pwd)))
                    {
                        reader.GetType().Field("encrypted").SetValue(reader, false);

                        using (var outStream = File.OpenWrite(outputFile))
                        {
                            using (var stamper = new PdfStamper(reader, outStream))
                            {
                                stamper.Close();
                            }
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error trying to decrypt file {0}: {1}", inputFile, ex.Message);
                }
            }
            return false;

        }

    }

}

using Castle.Core.Logging;
using System;

namespace Jarvis.DocumentStore.Jobs.PdfConverter.Converters
{
    public interface IPdfConverter
    {
        /// <summary>
        /// Tells if the file can be converted using only file name (usually extension)
        /// </summary>
        /// <param name="fileName">Name of the file, the file is NOT downloaded locally.</param>
        /// <returns></returns>
        Boolean CanConvert(String fileName);

        Boolean Convert(String inputFileName, String outputFileName);
    }

    public class PdfConverterBase
    {
        public ILogger Logger { get; set; }
    }
}

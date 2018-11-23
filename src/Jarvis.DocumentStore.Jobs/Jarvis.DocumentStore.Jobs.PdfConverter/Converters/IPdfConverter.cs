using Castle.Core.Logging;
using System;

namespace Jarvis.DocumentStore.Jobs.PdfConverter.Converters
{
    public interface IPdfConverter
    {
        Boolean CanConvert(String fileName);

        Boolean Convert(String inputFileName, String outputFileName);
    }

    public class PdfConverterBase
    {
        public ILogger Logger { get; set; }
    }
}

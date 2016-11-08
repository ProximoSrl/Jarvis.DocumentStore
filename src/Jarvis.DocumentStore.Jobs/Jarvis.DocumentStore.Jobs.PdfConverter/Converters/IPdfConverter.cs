using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

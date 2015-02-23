using System;
using java.io;
using org.apache.tika.io;
using org.apache.tika.metadata;
using org.apache.tika.parser;
using org.apache.tika.sax;
using TikaOnDotNet;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public class TikaNetAnalyzer : ITikaAnalyzer
    {
        public string GetHtmlContent(string filePath)
        {
            try
            {
                var file = new File(filePath);
                return this.Extract((Func<Metadata, InputStream>)(metadata =>
                {
                    var tikaInputStream = TikaInputStream.get(file, metadata);
                    return (InputStream)tikaInputStream;
                }));
            }
            catch (System.Exception ex)
            {
                throw new TextExtractionException(
                    StringExtrensions.ToFormat(
                        "Extraction of text from the file '{0}' failed.", 
                        new object[]{(object) filePath}
                        ), ex
                    );
            }
        }

        private string Extract(Func<Metadata, InputStream> streamFactory)
        {
            try
            {
                var autoDetectParser = new AutoDetectParser();
                var metadata = new Metadata();
                var handler = new ToXMLContentHandler();

                using (InputStream inputStream = streamFactory(metadata))
                {
                    try
                    {
                        autoDetectParser.parse(
                            inputStream, 
                            handler,
                            metadata
                            );
                    }
                    finally
                    {
                        inputStream.close();
                    }
                }

                var content = handler.ToString();
                return content;
            }
            catch (System.Exception ex)
            {
                throw new TextExtractionException("Extraction failed.", ex);
            }
        }
    }
}
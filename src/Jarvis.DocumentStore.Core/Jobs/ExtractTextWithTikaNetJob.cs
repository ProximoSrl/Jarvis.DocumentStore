using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using java.io;
using java.lang;
using java.net;
using javax.xml.transform;
using javax.xml.transform.sax;
using javax.xml.transform.stream;
using org.apache.tika.io;
using org.apache.tika.metadata;
using org.apache.tika.parser;
using org.apache.tika.sax;
using Quartz;
using TikaOnDotNet;

namespace Jarvis.DocumentStore.Core.Jobs
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

        private static TextExtractionResult assembleExtractionResult(string text, Metadata metadata)
        {
            Dictionary<string, string> dictionary = Enumerable.ToDictionary<string, string, string>((IEnumerable<string>)metadata.names(), (Func<string, string>)(name => name), (Func<string, string>)(name => string.Join(", ", metadata.getValues(name))));
            string str = dictionary["Content-Type"];
            return new TextExtractionResult()
            {
                Text = text,
                ContentType = str,
                Metadata = (IDictionary<string, string>)dictionary
            };
        }

    }

    public class ExtractTextWithTikaNetJob : AbstractTikaJob
    {
        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaNetAnalyzer();
        }
    }
}
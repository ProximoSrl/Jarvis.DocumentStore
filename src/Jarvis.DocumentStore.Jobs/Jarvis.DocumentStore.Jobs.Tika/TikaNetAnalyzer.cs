using System;
using System.Collections.Generic;
using Castle.Core.Logging;
using java.io;
using java.util;
using org.apache.tika.io;
using org.apache.tika.metadata;
using org.apache.tika.parser;
using org.apache.tika.sax;
using TikaOnDotNet;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    /// <summary>
    /// Class to wrap tika access.
    /// </summary>
    public class TikaNetAnalyzer : ITikaAnalyzer
    {
        public ILogger Logger = NullLogger.Instance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="password">Optional password to open the file.</param>
        /// <returns></returns>
        public string GetHtmlContent(string filePath, String password)
        {
            try
            {
                Logger.DebugFormat("GetHtmlContent for filePath: {0}", filePath);
                var file = new File(filePath);
                return this.Extract((Func<Metadata, InputStream>)(metadata =>
                {
                    Logger.DebugFormat("Extract metadata for {0}", filePath);
                    if (!String.IsNullOrEmpty(password))
                    {
                        metadata.add("org.apache.tika.parser.pdf.password", password);
                    }

                    var tikaInputStream = TikaInputStream.get(file, metadata);
                    Logger.DebugFormat("Return tikaInputStream for {0}", filePath);
                    return (InputStream)tikaInputStream;
                }));
            }
            catch (System.Exception ex)
            {
                Logger.ErrorFormat(ex, "Error on GetHtmlContent for {0}", filePath);
                throw new TextExtractionException(
                    StringExtrensions.ToFormat(
                        "Extraction of text from the file '{0}' failed.",
                        new object[] { (object)filePath }
                        ), ex
                    );
            }
        }

        private string Extract(Func<Metadata, InputStream> streamFactory)
        {
            try
            {
                Logger.Debug("Autodetect parser");
                var autoDetectParser = new AutoDetectParser();
                Logger.Debug("MetaData");
                var metadata = new Metadata();
                Logger.Debug("ToXMLContentHandler");
                var handler = new ToXMLContentHandler();

                Logger.Debug("Reading stream");
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
                        Logger.Debug("Closing Stream after auto-detect parser");
                        inputStream.close();
                    }
                }

                var content = handler.ToString();
                return content;
            }
            catch (System.Exception ex)
            {
                Logger.ErrorFormat(ex,"Extract error: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    Logger.ErrorFormat(ex.InnerException, "Extract inner error: {0}", ex.InnerException.Message);
                }

                throw new TextExtractionException("Extraction failed.", ex);
            }
        }

        public string Describe()
        {
            return "Tika on DotNet based extractor";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using java.io;
using java.net;
using NUnit.Framework;
using org.apache.tika.metadata;
using org.apache.tika.parser;
using org.apache.tika.sax;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    //[TestFixture]
    //public class TikaNetHtmlExtractionTest
    //{
    //    [Test]
    //    public void Extract_html()
    //    {
    //        string fileName = TestConfig.PathToDocumentPdf;

    //         ContentHandler handler = new ToXMLContentHandler();
        
    //    InputStream stream = ContentHandlerExample.class.getResourceAsStream("test.doc");
    //    AutoDetectParser parser = new AutoDetectParser();
    //    Metadata metadata = new Metadata();
    //    try {
    //        parser.parse(stream, handler, metadata);
    //        return handler.toString();
    //    } finally {
    //        stream.close();
    //    }

    //    }
    //}
}

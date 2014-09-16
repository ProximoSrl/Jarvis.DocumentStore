using System;
using System.IO;

namespace Jarvis.ImageService.Core.Tests.PipelineTests
{
    public static class SampleData
    {
        static readonly string DocumentsFolder;

        static SampleData()
        {
            DocumentsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Docs");
        }

        public static string PathToDocumentPdf {
            get { return Path.Combine(DocumentsFolder, "Document.pdf"); }
        }

        public static string PathToATextDocument{
            get { return Path.Combine(DocumentsFolder, "A text document.txt"); }
        }
    }
}
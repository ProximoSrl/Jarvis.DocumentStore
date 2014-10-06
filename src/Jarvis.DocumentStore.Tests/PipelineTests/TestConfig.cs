using System;
using System.IO;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    public static class TestConfig
    {
        static readonly string DocumentsFolder;

        static TestConfig()
        {
            DocumentsFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..\\..\\Docs")).FullName;
            ServerAddress = new Uri("http://localhost:5123");
        }

        public static Uri ServerAddress { get; private set; }

        public static string PathToDocumentPdf {
            get { return Path.Combine(DocumentsFolder, "Document.pdf"); }
        }

        public static string PathToDocumentCopyPdf{
            get { return Path.Combine(DocumentsFolder, "Document_Copy.pdf"); }
        }

        public static string PathToEml{
            get { return Path.Combine(DocumentsFolder, "eml sample.eml"); }
        }

        public static string PathToRTFDocument{
            get { return Path.Combine(DocumentsFolder, "RTF Document.rtf"); }
        }

        public static string PathToPowerpointShow{
            get { return Path.Combine(DocumentsFolder, "Powerpoint show.ppsx"); }
        }

        public static string PathToWordDocument{
            get { return Path.Combine(DocumentsFolder, "A Word Document.docx"); }
        }

        public static string PathToTextDocument{
            get { return Path.Combine(DocumentsFolder, "A text document.txt"); }
        }

        public static string PathToInvalidFile{
            get { return Path.Combine(DocumentsFolder, "file.invalid"); }
        }

        public static string PathToExcelDocument{
            get { return Path.Combine(DocumentsFolder, "An Excel Document.xlsx"); }
        }

        public static string PathToPowerpointDocument
        {
            get { return Path.Combine(DocumentsFolder, "A Powerpoint Document.pptx"); }
        }

        public static string PathToOpenDocumentText{
            get { return Path.Combine(DocumentsFolder, "An OpenDocument Text.odt"); }
        }

        public static string PathToOpenDocumentSpreadsheet{
            get { return Path.Combine(DocumentsFolder, "An OpenDocument Spreadsheet.ods"); }
        }

        public static string PathToOpenDocumentPresentation{
            get { return Path.Combine(DocumentsFolder, "An OpenDocument Presentation.odp"); }
        }

        public static string PathToHtml
        {
            get { return Path.Combine(DocumentsFolder, "Architecture.htm"); }
        }
    }
}
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

        public static string PathToWordDocument{
            get { return Path.Combine(DocumentsFolder, "A Word Document.docx"); }
        }

        public static string PathToTextDocument{
            get { return Path.Combine(DocumentsFolder, "A text document.txt"); }
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
        
    }
}
using System;
using System.IO;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    public static class TestConfig
    {
        static readonly string DocumentsFolder;

        static TestConfig()
        {
            DocumentsFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\Docs")).FullName;
            ServerAddress = new Uri("http://localhost:5123");
            Tenant = "tests";
            DocsTenant = "docs";
            DemoTenant = "demo";
        }

        public static Uri ServerAddress { get; private set; }
        public static String Tenant { get; private set; }

        public static string PathToDocumentPdf
        {
            get { return Path.Combine(DocumentsFolder, "Document.pdf"); }
        }

        public static string PowerpointWithLinkedDocuments
        {
            get { return Path.Combine(DocumentsFolder, "KPC_QCI_Training_English_4_4_06.ppt"); }
        }

        public static string PathToDocumentCopyPdf
        {
            get { return Path.Combine(DocumentsFolder, "Document_Copy.pdf"); }
        }

        public static string PathToMultilanguageDocx
        {
            get { return Path.Combine(DocumentsFolder, "Multilanguage.docx"); }
        }

        public static string PathToMultilanguagePdf
        {
            get { return Path.Combine(DocumentsFolder, "Multilanguage.pdf"); }
        }

        public static string PathToEml
        {
            get { return Path.Combine(DocumentsFolder, "eml sample.eml"); }
        }

        public static string PathToMsg
        {
            get { return Path.Combine(DocumentsFolder, "outlook message.msg"); }
        }

        public static string PathToLoremIpsumPdf
        {
            get { return Path.Combine(DocumentsFolder, "Lorem ipsum.pdf"); }
        }

        public static string PathToRTFDocument
        {
            get { return Path.Combine(DocumentsFolder, "RTF Document.rtf"); }
        }

        public static string PathToPowerpointShow
        {
            get { return Path.Combine(DocumentsFolder, "Powerpoint show.ppsx"); }
        }

        public static string PathToWordDocument
        {
            get { return Path.Combine(DocumentsFolder, "A Word Document.docx"); }
        }

        public static string PathToMultipageWordDocument
        {
            get { return Path.Combine(DocumentsFolder, "Multipage Word.docx"); }
        }

        public static string PathToTextDocument
        {
            get { return Path.Combine(DocumentsFolder, "A text document.txt"); }
        }

        public static string PathToInvalidFile
        {
            get { return Path.Combine(DocumentsFolder, "file.invalid"); }
        }

        public static string PathToExcelDocument
        {
            get { return Path.Combine(DocumentsFolder, "An Excel Document.xlsx"); }
        }

        public static string PathToPowerpointDocument
        {
            get { return Path.Combine(DocumentsFolder, "A Powerpoint Document.pptx"); }
        }

        public static string PathToOpenDocumentText
        {
            get { return Path.Combine(DocumentsFolder, "An OpenDocument Text.odt"); }
        }

        public static string PathToOpenDocumentSpreadsheet
        {
            get { return Path.Combine(DocumentsFolder, "An OpenDocument Spreadsheet.ods"); }
        }

        public static string PathToOpenDocumentPresentation
        {
            get { return Path.Combine(DocumentsFolder, "An OpenDocument Presentation.odp"); }
        }

        public static string PathToHtml
        {
            get { return Path.Combine(DocumentsFolder, "Architecture.htm"); }
        }

        public static string PathToMediumJpg
        {
            get { return Path.Combine(DocumentsFolder, "medium_image.jpg"); }
        }

        public static string PathToDocumentPng
        {
            get { return Path.Combine(DocumentsFolder, "document_1.png"); }
        }

        public static string DocsTenant { get; private set; }
        public static string DemoTenant { get; private set; }

        public static string PathToLangFile(string lang)
        {
            var pathToFile = Path.Combine(DocumentsFolder, "lang", lang + ".txt");
            return pathToFile;
        }

        public static string ReadLangFile(string lang)
        {

            return File.ReadAllText(PathToLangFile(lang));
        }
    }
}
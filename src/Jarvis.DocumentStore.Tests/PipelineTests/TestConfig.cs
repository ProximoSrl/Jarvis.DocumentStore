using System;
using System.IO;
using Jarvis.DocumentStore.Core.Domain.Document;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    public static class TestConfig
    {
        public static readonly string DocumentsFolder;
        public static readonly String QueueFolder;
        public static readonly string TempFolder;
        static TestConfig()
        {
            DocumentsFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs")).FullName;
            var aDocTestFile = Path.Combine(DocumentsFolder, "A Word Document.docx");
            if (!Directory.Exists(DocumentsFolder) || !File.Exists(aDocTestFile))
            {
                //we do not have Docs folder, use doc folder on source.
                DocumentsFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\Docs")).FullName;
            }

            TempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            ServerAddress = new Uri("http://localhost:5123");
            Tenant = "tests";
            DocsTenant = "docs";
            DemoTenant = "demo";
            QueueFolder = GenerateQueueFolder();
        }

        public static Uri ServerAddress { get; private set; }
        public static String Tenant { get; private set; }

        public static string PathToDocumentPdf
        {
            get { return Path.Combine(DocumentsFolder, "Document.pdf"); }
        }

        public static string PathToPasswordProtectedPdf
        {
            get { return Path.Combine(DocumentsFolder, "passwordprotected.pdf"); }

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

        public static string PathToMsgWithAttachment
        {
            get { return Path.Combine(DocumentsFolder, "Mail with attachments.msg"); }
        }

        public static string PathToMsgWithComplexAttachment
        {
            get { return Path.Combine(DocumentsFolder, "mailWithcomplexAttachments.msg"); }
        }

        public static string PathToMsgWithComplexAttachmentAndZipFileWithFolders
        {
            get { return Path.Combine(DocumentsFolder, "MailWithMultipleAttach.msg"); }
        }

        public static string PathToEmlWithComplexAttachmentAndZipFileWithFolders
        {
            get { return Path.Combine(DocumentsFolder, "MailWithMultipleAttach.eml"); }
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

        public static string PathToMht
        {
            get { return Path.Combine(DocumentsFolder, "MimeHtml.mht"); }
        }


        public static string PathToSimpleHtmlFile
        {
            get { return Path.Combine(DocumentsFolder, "HTMLPage.html"); }
        }

        public static string PathToHtmlZip
        {
            get { return Path.Combine(DocumentsFolder, "Architecture.htmlzip"); }
        }

        public static string PathToMediumJpg
        {
            get { return Path.Combine(DocumentsFolder, "medium_image.jpg"); }
        }

        public static string PathToZipFile
        {
            get { return Path.Combine(DocumentsFolder, "zipped.zip"); }
        }

        public static string PathToZipFileWithFolders
        {
            get { return Path.Combine(DocumentsFolder, "ZipWithFolders.zip"); }
        }
        
        public static string PathToZipFileThatContainsOtherZip
        {
            get { return Path.Combine(DocumentsFolder, "ZipWithNestedZip.zip"); }
        }

        public static string PathToDocumentPng
        {
            get { return Path.Combine(DocumentsFolder, "document_1.png"); }
        }

        public static string PathToFileWithNumbers
        {
            get { return Path.Combine(DocumentsFolder, "WrongFiles\\FileWithNumbers.txt"); }
        }

        public static string PathTo7Zip
        {
            get { return Path.Combine(DocumentsFolder, "sample.7z"); }
        }

        public static string PathToRar
        {
            get { return Path.Combine(DocumentsFolder, "sample.rar"); }
        }

        public static string PathToLoremIpsumTxt
        {
            get { return Path.Combine(DocumentsFolder, "lorem.txt"); }
        }

        public static string DocsTenant { get; private set; }
        public static string DemoTenant { get; private set; }
        private  static string GenerateQueueFolder()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Queue"); 
        }


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
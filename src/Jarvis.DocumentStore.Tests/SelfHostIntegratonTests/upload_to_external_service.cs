using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture, Explicit]
    [Category("Explicit")]
    public class Upload_to_external_service
    {
        private DocumentStoreServiceClient _docs;
        private DocumentStoreServiceClient _demo;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _docs = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.DocsTenant
            );

            _demo = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.DemoTenant
            );
        }

        [Test]
        public void Upload_pdf()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Rev_1")).Wait();
        }

        [Test]
        public void Upload_pdf_copyHandle()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("docs_to_copy")).Wait();
            Thread.Sleep(2000);
            _docs.CopyHandleAsync(DocumentHandle.FromString("docs_to_copy"), DocumentHandle.FromString("docs_to_copy_copied")).Wait();

            _demo.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("demo_to_copy")).Wait();
            Thread.Sleep(2000);
            _demo.CopyHandleAsync(DocumentHandle.FromString("demo_to_copy"), DocumentHandle.FromString("demo_to_copy_copied")).Wait();
        }

        [Test]
        public void Upload_pdf_copyHandle_then_delete()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("docs_to_copy")).Wait();
            _docs.CopyHandleAsync(DocumentHandle.FromString("docs_to_copy"), DocumentHandle.FromString("docs_to_copy_copied")).Wait();
            _docs.DeleteAsync(DocumentHandle.FromString("docs_to_copy")).Wait();
        }

        [Test]
        public void Upload_txt_document()
        {
            _docs.UploadAsync(TestConfig.PathToLoremIpsumTxt, DocumentHandle.FromString("text_document")).Wait();
        }

        [Test]
        public void Upload_pdf_then_delete()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Revision_42")).Wait();
            Thread.Sleep(3000);
            _docs.DeleteAsync(DocumentHandle.FromString("Revision_42")).Wait();
        }

        [Test]
        public void Upload_many_docs()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Doc1")).Wait();
            _docs.UploadAsync(TestConfig.PathTo7Zip, DocumentHandle.FromString("Doc2")).Wait();
            _docs.UploadAsync(TestConfig.PathToEml, DocumentHandle.FromString("Doc3")).Wait();
        }

        [Test]
        public void Upload_many_docs_delete()
        {
            _docs.DeleteAsync(DocumentHandle.FromString("Doc1")).Wait();
            _docs.DeleteAsync(DocumentHandle.FromString("Doc2")).Wait();
            _docs.DeleteAsync(DocumentHandle.FromString("Doc3")).Wait();
        }

        [Test]
        public void Upload_many_pdf()
        {
            for (int i = 0; i < 100; i++)
            {
                _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Manypdf_" + i)).Wait();
            }
        }

        [Test]
        public void Upload_many_pdf_delete()
        {
            for (int i = 0; i < 100; i++)
            {
                _docs.DeleteAsync(DocumentHandle.FromString("Manypdf_" + i)).Wait();
            }
        }

        [Test]
        public void Remove_tika_from_pdf()
        {
            _docs.RemoveFormatFromDocument(DocumentHandle.FromString("Rev_1"), new DocumentFormat("tika")).Wait();
        }

        [Test]
        public void Upload__temp_pdf()
        {
            _docs.UploadAsync(@"c:\temp\temppdf.pdf", DocumentHandle.FromString("temp_pdf")).Wait();
        }

        [Test]
        public void Upload__temp_excel()
        {
            _docs.UploadAsync(@"c:\temp\excel.xlsx", DocumentHandle.FromString("temp_excel")).Wait();
        }

        [Test]
        public void Upload__temp_excel_xlsm()
        {
            _docs.UploadAsync(@"c:\temp\excel.xlsm", DocumentHandle.FromString("temp_excel_xlsm")).Wait();
        }

        [Test]
        public void Upload__temp_text()
        {
            _docs.UploadAsync(@"c:\temp\temp.txt", DocumentHandle.FromString("temp_txt")).Wait();
        }

        [Test]
        public void Upload_list_files_in_temp()
        {
            var fileList = File.ReadAllText(@"c:\temp\filelist.txt");
            foreach (var line in fileList.Split('\n'))
            {
                FileInfo finfo = new FileInfo(line);
                _docs.UploadAsync(finfo.FullName, DocumentHandle.FromString(finfo.Name)).Wait();
            }
        }

        [Test]
        public void Upload_pdf_with_password()
        {
            _docs.UploadAsync(TestConfig.PathToPasswordProtectedPdf, DocumentHandle.FromString("pdf_password")).Wait();
        }

        [Test]
        public void Zipped_file_upload()
        {
            _docs.UploadAsync(TestConfig.PathToZipFile, DocumentHandle.FromString("zipsimple")).Wait();
        }

        [Test]
        public void Upload_seven_zip()
        {
            _docs.UploadAsync(TestConfig.PathTo7Zip, DocumentHandle.FromString("7zip")).Wait();
        }

        [Test]
        public void Zipped_file_withFolders_upload()
        {
            _docs.UploadAsync(TestConfig.PathToZipFileWithFolders, DocumentHandle.FromString("zip_with_folders")).Wait();
        }

        /// <summary>
        /// I load a document zipped, then another document that contains the first document
        /// as zipped content, I want to be sure that de-duplication does not break attachment
        /// chain
        /// </summary>
        [Test]
        public void Zipped_files_sequence_for_deduplication()
        {
            _docs.UploadAsync(TestConfig.PathToZipFile, DocumentHandle.FromString("zipfile")).Wait();
            Thread.Sleep(4000); //Give time to attachment job to do its job, then upload
                                //a zip document that contains the first one.
            _docs.UploadAsync(TestConfig.PathToZipFileThatContainsOtherZip,
                DocumentHandle.FromString("zipcontainer")).Wait();
            //you can test on 
            //http://localhost:5123/docs/documents/attachments_fat/zipcontainer
            //http://localhost:5123/docs/documents/attachments/zipcontainer
        }

        [Test]
        public void Attachment_complex_email_upload()
        {
            _docs.UploadAsync(TestConfig.PathToMsgWithComplexAttachment, DocumentHandle.FromString("msg_with_complex_attach")).Wait();
        }

        [Test]
        public void Email_msg_with_zip_with_folders_upload()
        {
            _docs.UploadAsync(TestConfig.PathToMsgWithComplexAttachmentAndZipFileWithFolders, DocumentHandle.FromString("msg_with_zip_folder")).Wait();
        }

        [Test]
        public void Email_eml_with_zip_with_folders_upload()
        {
            _docs.UploadAsync(TestConfig.PathToEmlWithComplexAttachmentAndZipFileWithFolders, DocumentHandle.FromString("eml_with_zip_folder")).Wait();
        }

        [Test]
        public void Zip_with_nested_zip_file_upload()
        {
            _docs.UploadAsync(TestConfig.PathToZipFileThatContainsOtherZip, DocumentHandle.FromString("zipchain")).Wait();
        }

        [Test]
        public void zipped_file_delete()
        {
            _docs.DeleteAsync(DocumentHandle.FromString("zipsimple")).Wait();
        }

        [Test]
        public void Upload_ppt_with_link()
        {
            //the file for this test is in trello card https://trello.com/c/SKGrSdAQ/156-libreoffice-dialog
            _docs.UploadAsync("c:\\temp\\KPC_QCI_Training_English_4_4_06.ppt", DocumentHandle.FromString("Rev_1")).Wait();
        }

        [Test]
        public void Upload_lorem_ipsum()
        {
            _docs.UploadAsync(TestConfig.PathToLoremIpsumPdf, DocumentHandle.FromString("lorem")).Wait();
        }

        [Test]
        public void Upload_pdf_to_demo_and_docs_tenants()
        {
            Task.WaitAll(
                _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("docsRev_1")),
                _demo.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("demoRev_1"))
            );
        }

        [Test]
        public void Upload_same_pdf_with_two_handles()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Pdf_1")).Wait();
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Pdf_2")).Wait();
        }

        [Test]
        public void Upload_same_pdf_with_two_handles_then_reuse_second_handle()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("handle_1")).Wait();
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("handle_2")).Wait();
            // overwrite handle
            _docs.UploadAsync(TestConfig.PathToDocumentPng, DocumentHandle.FromString("handle_2")).Wait();
        }

        [Test]
        public void Upload_same_pdf_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Rev_" + x)))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void Upload_same_pdf_100_times_with_same_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("this_is_a_pdf")))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void Upload_doc()
        {
            _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc")).Wait();
        }

        [Test]
        public void Upload_doc_with_metadata()
        {
            _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc"),
                new Dictionary<String, object>()
                {
                    {"param1" , "this is a test"},
                    {"the answer", 42},
                }).Wait();
        }

        [Test]
        public void Upload_text_with_metadata()
        {
            _docs.UploadAsync(TestConfig.PathToTextDocument, DocumentHandle.FromString("txt_test"),
                new Dictionary<String, object>()
                {
                    {"param1" , "this is a test"},
                    {"the answer", 42},
                }).Wait();
        }

        [Test]
        public void Upload_doc_then_add_format_to_doc()
        {
            _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc_2")).Wait();
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.CreatedById = "tika";
            model.DocumentHandle = DocumentHandle.FromString("doc_2");
            model.PathToFile = TestConfig.PathToTextDocument;
            model.Format = new DocumentFormat(DocumentFormats.Tika);
            _docs.AddFormatToDocument(model, null).Wait();
        }

        [Test]
        public void Upload_same_doc_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc_" + x)))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void Upload_same_doc_100_times_with_same_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("this_is_a_document")))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void Upload_pdf_with_handleA_and_handleB()
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(_docs.UploadAsync(TestConfig.PathToDocumentCopyPdf, DocumentHandle.FromString("a")));
            Thread.Sleep(500);
            tasks.Add(_docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("b")));
            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public void Upload_html()
        {
            _docs.UploadAsync(TestConfig.PathToHtml, DocumentHandle.FromString("html")).Wait();
        }

        [Test]
        public void Upload_mime_html()
        {
            _docs.UploadAsync(TestConfig.PathToMht, DocumentHandle.FromString("mhtml")).Wait();
        }

        [Test]
        public void Upload_simple_html()
        {
            var taskFolder = @"c:\temp\dsqueue";

            DocumentImportData data = _docs.CreateDocumentImportData(
                Guid.NewGuid(),
                TestConfig.PathToSimpleHtmlFile,
                Path.GetFileName(TestConfig.PathToSimpleHtmlFile),
                DocumentHandle.FromString("simple-html-file"));
            data.DeleteAfterImport = false;
            var docsFile = Path.Combine(taskFolder, "doc_simple-html-file_" + DateTime.Now.Ticks);

            _docs.QueueDocumentImport(data, docsFile);
        }

        [Test]
        public void Upload_html_zipped()
        {
            var zipped = _docs.ZipHtmlPage(TestConfig.PathToHtml);

            _docs.UploadAsync(
               zipped,
               DocumentHandle.FromString("html_zip"),
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            ).Wait();
        }

        [Test]
        public void Upload_some_test_file()
        {
            var fileName = @"X:\puthereyourpath.txt";
            _docs.UploadAsync(fileName, DocumentHandle.FromString("some_test_file")).Wait();
        }

        [Test]
        public void Upload_excel()
        {
            _docs.UploadAsync(TestConfig.PathToExcelDocument, DocumentHandle.FromString("xlsx")).Wait();
        }

        [Test]
        public void Upload_ppt()
        {
            _docs.UploadAsync(TestConfig.PathToPowerpointDocument, DocumentHandle.FromString("pptx")).Wait();
        }

        [Test]
        public void Upload_pps()
        {
            _docs.UploadAsync(TestConfig.PathToPowerpointShow, DocumentHandle.FromString("ppsx")).Wait();
        }

        [Test]
        public void Upload_odt()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentText, DocumentHandle.FromString("odt")).Wait();
        }

        [Test]
        public void Upload_ods()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, DocumentHandle.FromString("ods")).Wait();
        }

        [Test]
        public void Upload_odp()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentPresentation, DocumentHandle.FromString("odp")).Wait();
        }

        [Test]
        public void Upload_rtf()
        {
            _docs.UploadAsync(TestConfig.PathToRTFDocument, DocumentHandle.FromString("rtf")).Wait();
        }

        [Test]
        public void Upload_msg()
        {
            _docs.UploadAsync(TestConfig.PathToMsg, DocumentHandle.FromString("outlook_1")).Wait();
        }

        [Test]
        public void Upload_video()
        {
            _docs.UploadAsync("C:\\temp\\rainingblood.mp4", DocumentHandle.FromString("slayer_raining_blood")).Wait();
        }

        [Test]
        public void Upload_eml()
        {
            _docs.UploadAsync(TestConfig.PathToEml, DocumentHandle.FromString("eml_1")).Wait();
        }

        [Test]
        public void Upload_medium_jpg()
        {
            _docs.UploadAsync(TestConfig.PathToMediumJpg, DocumentHandle.FromString("jpg_1")).Wait();
        }

        [Test]
        public void Verify_get_of_feed()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("handle_2")).Wait();
            var feed = _docs.GetFeed(0, 20);
            Assert.That(feed.Count(), Is.GreaterThan(1));
        }

        [Test]
        public void Upload_mao_jpg_to_verify_resize()
        {
            _docs.UploadAsync(TestConfig.PathToMaoImage, DocumentHandle.FromString("mao")).Wait();
            var feed = _docs.GetFeed(0, 20);
            Assert.That(feed.Count(), Is.GreaterThan(1));
        }

        [Test]
        public void Verify_typed_get_of_feed()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("handle_3")).Wait();
            var feed = _docs.GetFeed(0, 20, HandleStreamEventTypes.DocumentCreated);
            Assert.That(feed.Count(), Is.GreaterThan(1));
            Assert.That(feed.All(d => d.EventType == HandleStreamEventTypes.DocumentCreated));
        }

        [Test]
        public void Upload_multi()
        {
            Task.WaitAll(
                _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("docx")),
                _docs.UploadAsync(TestConfig.PathToExcelDocument, DocumentHandle.FromString("xlsx")),
                _docs.UploadAsync(TestConfig.PathToPowerpointDocument, DocumentHandle.FromString("pptx")),
                _docs.UploadAsync(TestConfig.PathToPowerpointShow, DocumentHandle.FromString("ppsx")),
                _docs.UploadAsync(TestConfig.PathToOpenDocumentText, DocumentHandle.FromString("odt")),
                _docs.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, DocumentHandle.FromString("ods")),
                _docs.UploadAsync(TestConfig.PathToOpenDocumentPresentation, DocumentHandle.FromString("odp")),
                _docs.UploadAsync(TestConfig.PathToRTFDocument, DocumentHandle.FromString("rtf")),
                _docs.UploadAsync(TestConfig.PathToHtml, DocumentHandle.FromString("html"))
            );
        }

        [Test]
        public void Pdf_composer()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("pdfC1")).Wait();
            _docs.UploadAsync(TestConfig.PathToLoremIpsumTxt, DocumentHandle.FromString("pdfC2")).Wait();
            _docs.ComposeDocumentsAsync(
                DocumentHandle.FromString("pdfCresult"),
                "result.pdf",
                DocumentHandle.FromString("pdfC1"),
                DocumentHandle.FromString("pdfC2")).Wait();
        }

        [Test]
        public void Pdf_composer_with_format_without_pdf()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("pdfD1")).Wait();
            _docs.UploadAsync(TestConfig.PathToBinaryDocument, DocumentHandle.FromString("pdfD2")).Wait();
            _docs.UploadAsync(TestConfig.PathToLoremIpsumTxt, DocumentHandle.FromString("pdfD3")).Wait();
            _docs.ComposeDocumentsAsync(
                DocumentHandle.FromString("pdf-d-result"),
                "result.pdf",
                DocumentHandle.FromString("pdfD1"),
                DocumentHandle.FromString("pdfD2"),
                DocumentHandle.FromString("pdfD3")).Wait();
        }

        [Test]
        public void Upload_temp_zipFile()
        {
            _docs.UploadAsync("C:\\temp\\temp.zip", DocumentHandle.FromString("TempZip")).Wait();
        }
    }
}
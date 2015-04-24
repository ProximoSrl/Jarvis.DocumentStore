using System.Linq;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Threading;
using System.IO;


namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture, Explicit]
    public class upload_to_external_service
    {
        private DocumentStoreServiceClient _docs;
        private DocumentStoreServiceClient _demo;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
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
        public void _drop_all_tenants()
        {
            MongoDbTestConnectionProvider.DropAll();
        }

        [Test]
        public void upload_pdf()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Rev_1")).Wait();
        }

        [Test]
        public void upload__temp_pdf()
        {
            _docs.UploadAsync(@"c:\temp\temppdf.pdf", DocumentHandle.FromString("temp_pdf")).Wait();
        }

        [Test]
        public void upload_list_files_in_temp()
        {
            var fileList = File.ReadAllText(@"c:\temp\filelist.txt");
            foreach (var line in fileList.Split('\n'))
            {
                FileInfo finfo = new FileInfo(line);
                _docs.UploadAsync(finfo.FullName, DocumentHandle.FromString(finfo.Name)).Wait();
            }
            
        }

        [Test]
        public void upload_pdf_with_password()
        {
            _docs.UploadAsync(TestConfig.PathToPasswordProtectedPdf, DocumentHandle.FromString("pdf_password")).Wait();
        }

        [Test]
        public void zipped_file_upload()
        {
            _docs.UploadAsync(TestConfig.PathToZipFile, DocumentHandle.FromString("zipsimple")).Wait();
        }

        [Test]
        public void zipped_file_withFolders_upload()
        {
            _docs.UploadAsync(TestConfig.PathToZipFileWithFolders, DocumentHandle.FromString("zip_with_folders")).Wait();
        }

        /// <summary>
        /// I load a document zipped, then another document that contains the first document
        /// as zipped content, I want to be sure that de-duplication does not break attachment
        /// chain
        /// </summary>
        [Test]
        public void zipped_files_sequence_for_deduplication()
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
        public void attachment_complex_email_upload()
        {
            _docs.UploadAsync(TestConfig.PathToMsgWithComplexAttachment, DocumentHandle.FromString("msg_with_complex_attach")).Wait();
        }

        [Test]
        public void email_msg_with_zip_with_folders_upload()
        {
            _docs.UploadAsync(TestConfig.PathToMsgWithComplexAttachmentAndZipFileWithFolders, DocumentHandle.FromString("msg_with_zip_folder")).Wait();
        }

        [Test]
        public void email_eml_with_zip_with_folders_upload()
        {
            _docs.UploadAsync(TestConfig.PathToEmlWithComplexAttachmentAndZipFileWithFolders, DocumentHandle.FromString("eml_with_zip_folder")).Wait();
        }

        [Test]
        public void zip_with_nested_zip_file_upload()
        {
            _docs.UploadAsync(TestConfig.PathToZipFileThatContainsOtherZip, DocumentHandle.FromString("zipchain")).Wait();
        }

        [Test]
        public void zipped_file_delete()
        {
            _docs.DeleteAsync(DocumentHandle.FromString("zipsimple")).Wait();
        }

        [Test]
        public void upload_ppt_with_link()
        {
            //the file for this test is in trello card https://trello.com/c/SKGrSdAQ/156-libreoffice-dialog
            _docs.UploadAsync("c:\\temp\\KPC_QCI_Training_English_4_4_06.ppt", DocumentHandle.FromString("Rev_1")).Wait();

        }

        [Test]
        public void upload_lorem_ipsum()
        {
            _docs.UploadAsync(TestConfig.PathToLoremIpsumPdf, DocumentHandle.FromString("lorem")).Wait();
        }

        [Test]
        public void upload_pdf_to_demo_and_docs_tenants()
        {
            Task.WaitAll(
                _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Rev_1")),
                _demo.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Rev_1"))
            );
        }

        [Test]
        public void upload_same_pdf_with_two_handles()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Pdf_1")).Wait();
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Pdf_2")).Wait();
        }

        [Test]
        public void upload_same_pdf_with_two_handles_then_reuse_second_handle()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("handle_1")).Wait();
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("handle_2")).Wait();
            // overwrite handle
            _docs.UploadAsync(TestConfig.PathToDocumentPng, DocumentHandle.FromString("handle_2")).Wait();
        }

        [Test]
        public void upload_same_pdf_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("Rev_" + x)))
                .ToArray();

            Task.WaitAll(uploads);
        }


        [Test]
        public void upload_doc()
        {
            _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc")).Wait();
        }

        [Test]
        public void upload_doc_with_metadata()
        {
            _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc"),
                new Dictionary<String, object>() 
                { 
                    {"param1" , "this is a test"},
                    {"the answer", 42},
                }).Wait();
        }

        [Test]
        public void upload_text_with_metadata()
        {
            _docs.UploadAsync(TestConfig.PathToTextDocument, DocumentHandle.FromString("txt_test"),
                new Dictionary<String, object>() 
                { 
                    {"param1" , "this is a test"},
                    {"the answer", 42},
                }).Wait();
        }

        [Test]
        public void upload_doc_then_add_format_to_doc()
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
        public void upload_same_doc_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("doc_" + x)))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void upload_pdf_with_handleA_and_handleB()
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(_docs.UploadAsync(TestConfig.PathToDocumentCopyPdf, DocumentHandle.FromString("a")));
            Thread.Sleep(500);
            tasks.Add( _docs.UploadAsync(TestConfig.PathToDocumentPdf, DocumentHandle.FromString("b")));
            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public void upload_html()
        {
            _docs.UploadAsync(TestConfig.PathToHtml, DocumentHandle.FromString("html")).Wait();
        }

        [Test]
        public void upload_excel()
        {
            _docs.UploadAsync(TestConfig.PathToExcelDocument, DocumentHandle.FromString("xlsx")).Wait();
        }

        [Test]
        public void upload_ppt()
        {
            _docs.UploadAsync(TestConfig.PathToPowerpointDocument, DocumentHandle.FromString("pptx")).Wait();
        }

        [Test]
        public void upload_pps()
        {
            _docs.UploadAsync(TestConfig.PathToPowerpointShow, DocumentHandle.FromString("ppsx")).Wait();
        }

        [Test]
        public void upload_odt()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentText, DocumentHandle.FromString("odt")).Wait();
        }

        [Test]
        public void upload_ods()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, DocumentHandle.FromString("ods")).Wait();
        }

        [Test]
        public void upload_odp()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentPresentation, DocumentHandle.FromString("odp")).Wait();
        }

        [Test]
        public void upload_rtf()
        {
            _docs.UploadAsync(TestConfig.PathToRTFDocument, DocumentHandle.FromString("rtf")).Wait();
        }

        [Test]
        public void upload_msg()
        {
            _docs.UploadAsync(TestConfig.PathToMsg, DocumentHandle.FromString("outlook_1")).Wait();
        }

        [Test]
        public void upload_eml()
        {
            _docs.UploadAsync(TestConfig.PathToEml, DocumentHandle.FromString("eml_1")).Wait();
        }

        [Test]
        public void upload_medium_jpg()
        {
            _docs.UploadAsync(TestConfig.PathToMediumJpg, DocumentHandle.FromString("jpg_1")).Wait();
        }

        [Test]
        public void upload_multi()
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
        public async void upload_video()
        {
            await
                _docs.UploadAsync(
//                    @"c:\Users\andrea.balducci\Downloads\Placebo---The-Bitter-End-Live-At-Sziget-2014-large.mp4",
//                        new DocumentHandle("bitter_end"));
                    @"C:\Downloads\Video\[MP4 720p] Placebo Live @ Sziget 2014 [Full Concert].mp4",
                    new DocumentHandle("sziget"));
        }
    }
}
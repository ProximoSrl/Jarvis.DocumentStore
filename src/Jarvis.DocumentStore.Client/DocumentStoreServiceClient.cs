using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Shared.Serialization;
using Newtonsoft.Json;
using Jarvis.DocumentStore.Shared;
using Jarvis.DocumentStore.Shared.Jobs;
using System.Linq;
using Newtonsoft.Json.Linq;

#if DisablePriLongPath
using Path = System.IO.Path;
using File = System.IO.File;
#else
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
#endif


namespace Jarvis.DocumentStore.Client
{
    public class OpenOptions
    {
        public string FileName { get; set; }
        public Int64? RangeFrom { get; set; }
        public Int64? RangeTo { get; set; }
        public bool SkipContent { get; set; }
    }

    /// <summary>
    /// DocumentStore client
    /// </summary>
    public class DocumentStoreServiceClient
    {
        public string Tenant { get; private set; }
        readonly Uri _documentStoreUri;
        public static readonly DocumentFormat OriginalFormat = new DocumentFormat("original");
        public string TempFolder { get; set; }

        /// <summary>
        /// Create a new DocumentStore Client
        /// </summary>
        /// <param name="documentStoreUri">base uri</param>
        /// <param name="tenant">tenantId</param>
        public DocumentStoreServiceClient(Uri documentStoreUri, string tenant)
        {
            Tenant = tenant;
            _documentStoreUri = documentStoreUri;
            TempFolder = Path.Combine(Path.GetTempPath(), "jarvis.client");
        }

        /// <summary>
        /// Zip an html page with images / scripts subfolder
        /// </summary>
        /// <param name="pathToFile">path to html file</param>
        /// <returns>path to zipped file</returns>
        public string ZipHtmlPage(string pathToFile)
        {
            if (!Directory.Exists(TempFolder))
                Directory.CreateDirectory(TempFolder);

            string pathToZip = Path.ChangeExtension(Path.Combine(
                TempFolder,
                Path.GetFileName(pathToFile)
            ), ".htmlzip");

            if (File.Exists(pathToZip)) File.Delete(pathToZip);

            using (ZipArchive zip = ZipFile.Open(pathToZip, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(pathToFile, Path.GetFileName(pathToFile));
                var attachmentFolder = FindAttachmentFolder(pathToFile);
                if (attachmentFolder != null)
                {
                    var subfolderName = Path.GetFileName(attachmentFolder);
                    foreach (var fname in Directory.GetFiles(attachmentFolder))
                    {
                        // filter some extensions?
                        zip.CreateEntryFromFile(
                            fname,
                            subfolderName + "/" + Path.GetFileName(fname)
                        );
                    }
                }
            }

            return pathToZip;
        }

        /// <summary>
        /// Stategy for attachment folder identification
        /// </summary>
        /// <param name="pathToFile">path to html file</param>
        /// <returns>path to html attachments</returns>
        static string FindAttachmentFolder(string pathToFile)
        {
            var attachmentFolder = Path.Combine(
                Path.GetDirectoryName(pathToFile),
                Path.GetFileNameWithoutExtension(pathToFile) + "_files"
                );

            if (Directory.Exists(attachmentFolder))
            {
                return attachmentFolder;
            }

            return null;
        }

        /// <summary>
        /// upload a document
        /// </summary>
        /// <param name="fileNameWithExtension">File name with extension</param>
        /// <param name="documentHandle">Document handle</param>
        /// <param name="inputStream">Input stream</param>
        /// <param name="customData">Custom Data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        public async Task<UploadedDocumentResponse> UploadAsync(
            string fileNameWithExtension,
            DocumentHandle documentHandle,
            Stream inputStream,
            IDictionary<string, object> customData = null)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle);
            return await DoUpload(endPoint, fileNameWithExtension, inputStream, customData);
        }

        /// <summary>
        /// Create a new document as an attach of existing document
        /// </summary>
        /// <param name="fileNameWithExtension">File name with extension</param>
        /// <param name="fatherDocumentHandle">Document handle</param>
        /// <param name="inputStream">Input stream</param>
        /// <param name="attachSource"></param>
        /// <param name="customData">Custom Data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        public async Task<UploadedDocumentResponse> UploadAttachmentAsync(
            string fileNameWithExtension,
            DocumentHandle fatherDocumentHandle,
            Stream inputStream,
            String attachSource,
            String relativePath,
            IDictionary<string, object> customData = null)
        {
            if (customData == null) customData = new Dictionary<String, Object>();
            customData[AddAttachmentToHandleParameters.Source] = attachSource;
            customData[JobsConstants.AttachmentRelativePath] = relativePath;
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + fatherDocumentHandle + "/attach/" + attachSource);
            return await DoUpload(endPoint, fileNameWithExtension, inputStream, customData);
        }

        public async Task<UploadedDocumentResponse> UploadAttachmentAsync(
           string pathToFile,
           DocumentHandle fatherDocumentHandle,
           String attachSource,
           String relativePath,
           IDictionary<string, object> customData = null)
        {
            if (customData == null) customData = new Dictionary<String, Object>();
            customData[AddAttachmentToHandleParameters.Source] = attachSource;
            customData[JobsConstants.AttachmentRelativePath] = relativePath;
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + fatherDocumentHandle + "/attach/" + attachSource);
            return await UploadFromFile(endPoint, pathToFile, customData);
        }

        public async Task<UploadedDocumentResponse> UploadAttachmentAsync(
           string pathToFile,
           String queueName,
           String jobId,
           String attachSource,
           String relativePath,
           IDictionary<string, object> customData = null)
        {
            if (customData == null) customData = new Dictionary<String, Object>();
            customData[AddAttachmentToHandleParameters.Source] = attachSource;
            customData[JobsConstants.AttachmentRelativePath] = relativePath;
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/jobs/attach/" + queueName + "/" + jobId + "/" + attachSource);
            return await UploadFromFile(endPoint, pathToFile, customData);
        }

        /// <summary>
        /// Upload a document
        /// </summary>
        /// <param name="pathToFile">Path to local document</param>
        /// <param name="documentHandle">Document handle</param>
        /// <param name="customData">Custom data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        public async Task<UploadedDocumentResponse> UploadAsync(
            string pathToFile,
            DocumentHandle documentHandle,
            IDictionary<string, object> customData = null)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle);
            return await UploadFromFile(endPoint, pathToFile, customData);
        }

        /// <summary>
        /// copy an handle to another handle without forcing the client
        /// to download and re-upload the same file
        /// </summary>
        /// <param name="originalHandle">Handle you want to copy</param>
        /// <param name="copiedHandle">Copied handle that will point to the very
        /// same content of the original one.</param>
        /// <returns></returns>
        public async Task<String> CopyHandleAsync(
            DocumentHandle originalHandle,
            DocumentHandle copiedHandle)
        {
            using (var client = new HttpClient())
            {
                var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/" + originalHandle + "/copy/" + copiedHandle);
                return await client.GetStringAsync(resourceUri);
            }            
        }


        private async Task<UploadedDocumentResponse> UploadFromFile(Uri endPoint, string pathToFile, IDictionary<string, object> customData)
        {
            var fileExt = Path.GetExtension(pathToFile).ToLowerInvariant();
            if (fileExt == ".html" || fileExt == ".htm")
            {
                var zippedFile = ZipHtmlPage(pathToFile);
                try
                {
                    return await InnerUploadAsync(endPoint, zippedFile, customData);

                }
                finally
                {
                    File.Delete(zippedFile);
                }
            }

            return await InnerUploadAsync(endPoint, pathToFile, customData);
        }

        /// <summary>
        /// Utility method for uploads
        /// </summary>
        /// <param name="endPoint">The endopoint used to upload the file</param>
        /// <param name="pathToFile">Path to local document</param>
        /// <param name="customData">Custom data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        private async Task<UploadedDocumentResponse> InnerUploadAsync(
            Uri endPoint,
            string pathToFile,
            IDictionary<string, object> customData
        )
        {
            string fileNameWithExtension = Path.GetFileName(pathToFile);
            using (var sourceStream = File.OpenRead(pathToFile))
            {
                return await DoUpload(endPoint, fileNameWithExtension, sourceStream, customData);
            }
        }

        async Task<UploadedDocumentResponse> DoUpload(
            Uri endPoint,
            string fileNameWithExtension,
            Stream inputStream,
            IDictionary<string, object> customData)
        {
            string fileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);

            using (var client = new HttpClient())
            {
                using (
                    var content =
                        new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    content.Add(
                        new StreamContent(inputStream),
                        fileName, fileNameWithExtension
                    );

                    if (customData != null)
                    {
                        var stringContent = new StringContent(await ToJsonAsync(customData));
                        content.Add(stringContent, "custom-data");
                    }

                    using (var message = await client.PostAsync(endPoint, content))
                    {
                        var json = await message.Content.ReadAsStringAsync();
                        message.EnsureSuccessStatusCode();
                        return JsonConvert.DeserializeObject<UploadedDocumentResponse>(json);
                    }
                }
            }
        }

        public async Task<UploadedDocumentResponse> AddFormatToDocument(
            AddFormatFromFileToDocumentModel model,
            IDictionary<string, object> customData = null)
        {
            using (var sourceStream = File.OpenRead(model.PathToFile))
            {
                using (var client = new HttpClient())
                {
                    using (
                        var content =
                            new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                    {

                        var fileInfo = new FileInfo(model.PathToFile);
                        content.Add(
                            new StreamContent(sourceStream),
                            "stream",
                            fileInfo.Name
                        );

                        customData = customData ?? new Dictionary<String, Object>();
                        customData.Add(AddFormatToDocumentParameters.CreatedBy, model.CreatedById);
                        customData.Add(AddFormatToDocumentParameters.DocumentHandle, model.DocumentHandle);
                        customData.Add(AddFormatToDocumentParameters.JobId, model.JobId);
                        customData.Add(AddFormatToDocumentParameters.QueueName, model.QueueName);
                        customData.Add(AddFormatToDocumentParameters.Format, model.Format);

                        var stringContent = new StringContent(JsonConvert.SerializeObject(customData));
                        content.Add(stringContent, "custom-data");

                        var modelFormat = model.Format == null ? "null" : model.Format.ToString();
                        var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/addformat/" + modelFormat);

                        using (var message = await client.PostAsync(endPoint, content))
                        {
                            var json = await message.Content.ReadAsStringAsync();
                            message.EnsureSuccessStatusCode();
                            return JsonConvert.DeserializeObject<UploadedDocumentResponse>(json);
                        }
                    }
                }
            }
        }

        public async Task<UploadedDocumentResponse> AddFormatToDocument(
            AddFormatFromObjectToDocumentModel model,
            IDictionary<string, object> customData = null)
        {
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes((model.StringContent))))
            {

                using (var client = new HttpClient())
                {
                    using (
                        var content =
                            new MultipartFormDataContent("Upload----" +
                                                         DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                    {
                        content.Add(
                            new StreamContent(sourceStream),
                            "stream",
                            model.FileName
                        );

                        customData = customData ?? new Dictionary<String, Object>();
                        customData.Add(AddFormatToDocumentParameters.CreatedBy, model.CreatedById);
                        customData.Add(AddFormatToDocumentParameters.DocumentHandle, model.DocumentHandle);
                        customData.Add(AddFormatToDocumentParameters.JobId, model.JobId);
                        customData.Add(AddFormatToDocumentParameters.QueueName, model.QueueName);
                        customData.Add(AddFormatToDocumentParameters.Format, model.Format);

                        var stringContent = new StringContent(JsonConvert.SerializeObject(customData));
                        content.Add(stringContent, "custom-data");

                        var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/addformat/" + model.Format);

                        using (var message = await client.PostAsync(endPoint, content))
                        {
                            var json = await message.Content.ReadAsStringAsync();
                            message.EnsureSuccessStatusCode();
                            return JsonConvert.DeserializeObject<UploadedDocumentResponse>(json);
                        }
                    }
                }
            }
        }

        public async Task RemoveFormatFromDocument(DocumentHandle handle, DocumentFormat documentFormat)
        {
            using (var client = new HttpClient())
            {
                var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/" + handle + "/" + documentFormat);

                await client.DeleteAsync(resourceUri);
            }
        }


        /// <summary>
        /// Serialize custom data to json string
        /// </summary>
        /// <param name="data">custom data</param>
        /// <returns>json representation of custom data</returns>
        private Task<string> ToJsonAsync(IDictionary<string, object> data)
        {
            return Task.Factory.StartNew(() => JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// Deserialize custom data from json string
        /// </summary>
        /// <param name="data">json representation of custom data</param>
        /// <param name="settings">serializer settings</param>
        /// <returns>custom data</returns>
        private Task<T> FromJsonAsync<T>(string data, JsonSerializerSettings settings = null)
        {
            return Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(data, settings));
        }

        /// <summary>
        /// Deserialize custom data from json string
        /// </summary>
        /// <param name="data">json representation of custom data</param>
        /// <param name="settings">serializer settings</param>
        /// <returns>custom data</returns>
        private T FromJson<T>(string data, JsonSerializerSettings settings = null)
        {
            return JsonConvert.DeserializeObject<T>(data, settings);
        }

        /// <summary>
        /// Retrieve custom data from DocumentStore
        /// </summary>
        /// <param name="documentHandle">Document handle</param>
        /// <returns>Custom data</returns>
        public async Task<IDictionary<string, object>> GetCustomDataAsync(DocumentHandle documentHandle)
        {
            using (var client = new HttpClient())
            {
                var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle + "/@customdata");

                var json = await client.GetStringAsync(endPoint);
                return await FromJsonAsync<IDictionary<string, object>>(json);
            }
        }

        public async Task<string> GetFileNameAsync(DocumentHandle documentHandle)
        {
            using (var client = new HttpClient())
            {
                var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle + "/@filename");

                var json = await client.GetStringAsync(endPoint);
                var response = FromJson<JObject>(json);
                return response["fileName"].Value<String>();
            }
        }

        /// <summary>
        /// Open a file on DocumentStore
        /// </summary>
        /// <param name="documentHandle">Document handle</param>
        /// <param name="format">Document format</param>
        /// <param name="options">Open options</param>
        /// <returns>A document format reader</returns>
        public DocumentFormatReader OpenRead(DocumentHandle documentHandle, DocumentFormat format = null, OpenOptions options = null)
        {
            format = format ?? OriginalFormat;
            var relativeUri = Tenant + "/documents/" + documentHandle + "/" + format;
            if (options != null && !string.IsNullOrWhiteSpace(options.FileName))
                relativeUri = relativeUri + "/" + options.FileName;

            var endPoint = new Uri(_documentStoreUri, relativeUri);
            return new DocumentFormatReader(endPoint, options);
        }

        /// <summary>
        /// Open a binary content, it is necessary for workers out of process that does not care
        /// about <see cref="DocumentHandle" /> but have a reference to a job id related to a blob
        /// </summary>
        /// <param name="queueName">The name of the queue that is executing the job</param>
        /// <param name="jobId">The id of the job.</param>
        /// <returns></returns>
        public DocumentFormatReader OpenBlobIdForRead(String queueName, String jobId)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/jobs/blob/" + queueName + "/" + jobId);
            return new DocumentFormatReader(endPoint);
        }

        /// <summary>
        /// Delete a document from Document Store
        /// </summary>
        /// <param name="handle">Document handle</param>
        /// <returns>Task</returns>
        public async Task DeleteAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/" + handle);
            using (var client = new HttpClient())
            {
                await client.DeleteAsync(resourceUri);
            }
        }

        /// <summary>
        /// Delete all attachment of a given handle, specifying the source.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task DeleteAttachmentsAsync(DocumentHandle handle, String source)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/attachments/" + handle + "/" + source);
            using (var client = new HttpClient())
            {
                await client.DeleteAsync(resourceUri);
            }
        }

        /// <summary>
        /// Get the formats available for a document handle
        /// </summary>
        /// <param name="handle">document handles</param>
        /// <returns>Document formats</returns>
        public async Task<DocumentFormats> GetFormatsAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/" + handle);
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(resourceUri);
                var d = await FromJsonAsync<IDictionary<DocumentFormat, Uri>>(json);
                return new DocumentFormats(d);
            }
        }

        public async Task<DocumentAttachments> GetAttachmentsAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/attachments/" + handle);
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(resourceUri);
                var d = await FromJsonAsync<ClientAttachmentInfo[]>(json);
                return new DocumentAttachments(d);
            }
        }

        public async Task<DocumentAttachmentsFat> GetAttachmentsFatAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/attachments_fat/" + handle);
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(resourceUri);
                var d = await FromJsonAsync<List<DocumentAttachmentsFat.AttachmentInfo>>(json);
                return new DocumentAttachmentsFat(d);
            }
        }

        /// <summary>
        /// Get document content (typed)
        /// </summary>
        /// <param name="handle">document handle</param>
        /// <returns><see cref="DocumentFormat"/>document content</returns>
        public async Task<DocumentContent> GetContentAsync(DocumentHandle handle)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + handle + "/content");
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(endPoint);
                return await FromJsonAsync<DocumentContent>(json, PocoSerializationSettings.Default);
            }
        }

        public DocumentImportData CreateDocumentImportData(
            Guid taskId,
            string fileUri,
            string fileName,
            DocumentHandle handle,
            DocumentFormat format = null)
        {
            format = format ?? OriginalFormat;
            return new DocumentImportData(new Uri(fileUri), fileName, handle, format, Tenant, taskId);
        }

        public void QueueDocumentImport(DocumentImportData did, string pathToFile)
        {
            string fileName = pathToFile + ".dsimport";
            string folder = Path.GetDirectoryName(fileName);

            if (folder == null)
                throw new Exception("Invalid folder");

            string json = JsonConvert.SerializeObject(did, Formatting.Indented);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(fileName, json);
        }


        /// <summary>
        /// Retrieve custom data from DocumentStore
        /// </summary>
        /// <param name="documentHandle">Document handle</param>
        /// <returns>Custom data</returns>
        public async Task<IEnumerable<ClientFeed>> GetFeedAsync(Int64 startFeed, Int32 numOfFeeds, params HandleStreamEventTypes[] types)
        {
            using (var client = new HttpClient())
            {
                Uri endPoint = GenerateUriForFeed(startFeed, numOfFeeds, types);
                var json = await client.GetStringAsync(endPoint);
                return await FromJsonAsync<IEnumerable<ClientFeed>>(json);
            }
        }

        /// <summary>
        /// Retrieve custom data from DocumentStore
        /// </summary>
        /// <param name="documentHandle">Document handle</param>
        /// <returns>Custom data</returns>
        public IEnumerable<ClientFeed> GetFeed(Int64 startFeed, Int32 numOfFeeds, params HandleStreamEventTypes[] types)
        {
            using (var client = new WebClient())
            {
                Uri endPoint = GenerateUriForFeed(startFeed, numOfFeeds, types);
                var json = client.DownloadString(endPoint);
                return FromJson<IEnumerable<ClientFeed>>(json);
            }
        }

        public async Task<QueuedJobInfo[]> GetJobsAsync(DocumentHandle handle)
        {
            var getJobsUri = GenerateUriForGetJobs(handle);
            using (var client = new WebClient())
            {
                var result = await client.DownloadStringTaskAsync(getJobsUri);
                return GenerateArrayOfJobs(result);
            }
        }

        public QueuedJobInfo[] GetJobs(DocumentHandle handle)
        {
            var getJobsUri = GenerateUriForGetJobs(handle);
            using (var client = new WebClient())
            {
                var result = client.DownloadString(getJobsUri);
                return GenerateArrayOfJobs(result);
            }
        }


        public async Task<Boolean> ComposeDocumentsAsync(
            DocumentHandle resultingDocumentHandle,
            String resultingDocumentFileName, 
            params DocumentHandle[] documentList)
        {
            Uri composePdfUri = GenerateUriForComposePdf();
            using (var client = new WebClient())
            {
                ComposeDocumentsModel parameter = CreateParameterForComposeDocuments(resultingDocumentHandle, resultingDocumentFileName, documentList);
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = await client.UploadStringTaskAsync(composePdfUri, JsonConvert.SerializeObject(parameter));
                return CreateReturnObjectForComposeDocuments(result);
            }
        }

        public Boolean ComposeDocuments(
                DocumentHandle resultingDocumentHandle, 
                String resultingDocumentFileName,
                params DocumentHandle[] documentList)
        {
            Uri composePdfUri = GenerateUriForComposePdf();
            using (var client = new WebClient())
            {
                ComposeDocumentsModel parameter = CreateParameterForComposeDocuments(resultingDocumentHandle, resultingDocumentFileName, documentList);
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = client.UploadString(composePdfUri, JsonConvert.SerializeObject(parameter));
                return CreateReturnObjectForComposeDocuments(result);
            }
        }

        #region Helper functions

        private Uri GenerateUriForFeed(long startFeed, int numOfFeeds, HandleStreamEventTypes[] types)
        {
            var uriString = Tenant + "/feed/" + startFeed + "/" + numOfFeeds;
            if (types != null && types.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var type in types)
                {
                    sb.Append("types=" + (Int32)type + "&");
                }
                sb.Length -= 1;
                uriString = uriString + "?" + sb.ToString();
            }

            var endPoint = new Uri(_documentStoreUri, uriString);
            return endPoint;
        }
        private Uri GenerateUriForGetJobs(DocumentHandle handle)
        {
            return new Uri(_documentStoreUri, "queue/getJobs/" + Tenant + "/" + handle.ToString());
        }

        private static QueuedJobInfo[] GenerateArrayOfJobs(string result)
        {
            return JsonConvert.DeserializeObject<QueuedJobInfo[]>(result);
        }

        private Uri GenerateUriForComposePdf()
        {
            return new Uri(_documentStoreUri, Tenant + "/compose");
        }

        private static ComposeDocumentsModel CreateParameterForComposeDocuments(
            DocumentHandle resultingDocumentHandle, 
            String resultingDocumentFileName,
            DocumentHandle[] documentList)
        {
            return new ComposeDocumentsModel()
            {
                ResultingDocumentHandle = resultingDocumentHandle,
                ResultingDocumentFileName = resultingDocumentFileName,
                DocumentList = documentList,
            };
        }

        private static bool CreateReturnObjectForComposeDocuments(string result)
        {
            var resultJson = (JObject)JsonConvert.DeserializeObject(result);
            return resultJson["result"].Value<String>() == "ok";
        }


        #endregion

    }
}
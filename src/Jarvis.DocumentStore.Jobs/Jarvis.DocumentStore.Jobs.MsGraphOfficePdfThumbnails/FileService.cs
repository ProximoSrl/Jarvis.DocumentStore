﻿using Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails;
using MimeTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails
{
    public class FileService
    {
        private readonly AuthenticationService _authenticationService;
        private HttpClient _httpClient;

        public FileService(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        private async Task<HttpClient> CreateAuthorizedHttpClient()
        {
            if (_httpClient != null)
            {
                return _httpClient;
            }

            var token = await _authenticationService.GetAccessTokenAsync();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            return _httpClient;
        }

        public async Task<string> UploadStreamAsync(string path, Stream content, string fileFullPath)
        {
            var httpClient = await CreateAuthorizedHttpClient().ConfigureAwait(false);

            var contentType = MimeTypeMap.GetMimeType(Path.GetExtension(fileFullPath));
            string tmpFileName = $"{Guid.NewGuid()}{MimeTypeMap.GetExtension(contentType)}";
            string requestUrl = $"{path}root:/{tmpFileName}:/content";
            var requestContent = new StreamContent(content);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var response = await httpClient.PutAsync(requestUrl, requestContent).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var file = (JObject) JsonConvert.DeserializeObject(responseContent);
                return file["id"].Value<string>();
            }
            else
            {
                var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Upload file failed with status {response.StatusCode} and message {message}");
            }
        }

        public async Task<byte[]> DownloadConvertedFileAsync(string path, string fileId, string targetFormat)
        {
            var httpClient = await CreateAuthorizedHttpClient().ConfigureAwait(false);

            var requestUrl = $"{path}{fileId}/content?format={targetFormat}";
            var response = await httpClient.GetAsync(requestUrl).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            else
            {
                var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Download of converted file failed with status {response.StatusCode} and message {message}");
            }
        }

        public async Task<byte[]> DownloadFileThumbnailAsync(string path, string fileId)
        {
            var httpClient = await CreateAuthorizedHttpClient().ConfigureAwait(false);

            var requestUrl = $"{path}{fileId}/thumbnails";
            var response = await httpClient.GetAsync(requestUrl).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var doc = (JObject) JsonConvert.DeserializeObject(message);
                var largeThumb = ((JArray)doc["value"])[0]["large"]["url"].Value<string>();

                var thumb = await httpClient.GetAsync(largeThumb);

                return await thumb.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            else
            {
                var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Download of converted file failed with status {response.StatusCode} and message {message}");
            }
        }

        public async Task DeleteFileAsync(string path, string fileId)
        {
            var httpClient = await CreateAuthorizedHttpClient().ConfigureAwait(false);

            var requestUrl = $"{path}{fileId}";
            var response = await httpClient.DeleteAsync(requestUrl).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Delete file failed with status {response.StatusCode} and message {message}");
            }
        }
    }
}

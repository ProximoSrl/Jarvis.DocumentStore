using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Helpers
{
    /// <summary>
    /// Useful WebClient extensions methods to be able to correctly work with UTF-8
    /// </summary>
    public static class WebClientExtensions
    {
        public static string DownloadStringAwareOfEncoding(this WebClient webClient, string uri)
        {
            var rawData = webClient.DownloadData(uri);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static string DownloadStringAwareOfEncoding(this WebClient webClient, Uri uri)
        {
            var rawData = webClient.DownloadData(uri);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static string UploadStringAwareOfEncoding(this WebClient webClient, string uri, string payload, string method = "POST")
        {
            webClient.Encoding = Encoding.UTF8;
            var uploadData = Encoding.UTF8.GetBytes(payload);
            var rawData = webClient.UploadData(uri, method, uploadData);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static string UploadStringAwareOfEncoding(this WebClient webClient, Uri uri, string payload, string method = "POST")
        {
            webClient.Encoding = Encoding.UTF8;
            var uploadData = Encoding.UTF8.GetBytes(payload);
            var rawData = webClient.UploadData(uri, method, uploadData);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static async Task<string> DownloadStringAwareOfEncodingAsync(this WebClient webClient, string uri)
        {
            var rawData = await webClient.DownloadDataTaskAsync(uri).ConfigureAwait(false);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static async Task<string> DownloadStringAwareOfEncodingAsync(this WebClient webClient, Uri uri)
        {
            var rawData = await webClient.DownloadDataTaskAsync(uri).ConfigureAwait(false);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static async Task<string> UploadStringAwareOfEncodingAsync(this WebClient webClient, string uri, string payload, string method = "POST")
        {
            webClient.Encoding = Encoding.UTF8;
            var uploadData = Encoding.UTF8.GetBytes(payload);
            var rawData = await webClient.UploadDataTaskAsync(uri, method, uploadData).ConfigureAwait(false);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static async Task<string> UploadStringAwareOfEncodingAsync(this WebClient webClient, Uri uri, string payload, string method = "POST")
        {
            webClient.Encoding = Encoding.UTF8;
            var uploadData = Encoding.UTF8.GetBytes(payload);
            var rawData = await webClient.UploadDataTaskAsync(uri, method, uploadData).ConfigureAwait(false);
            var encoding = GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);
            return encoding.GetString(rawData);
        }

        public static Encoding GetEncodingFrom(
            NameValueCollection responseHeaders,
            Encoding defaultEncoding = null)
        {
            try
            {
                if (responseHeaders == null)
                {
                    return defaultEncoding; // Safe
                }

                var contentType = responseHeaders["Content-Type"];
                if (contentType == null)
                {
                    return defaultEncoding;
                }

                var contentTypeParsed = new ContentType(contentType);
                if (!String.IsNullOrEmpty(contentTypeParsed.CharSet))
                {
                    return Encoding.GetEncoding(contentTypeParsed.CharSet);
                }

                return defaultEncoding;

            }
            catch (ArgumentException)
            {
                // Ignore all argument errors and return the default encoding
                return defaultEncoding;
            }
        }
    }
}

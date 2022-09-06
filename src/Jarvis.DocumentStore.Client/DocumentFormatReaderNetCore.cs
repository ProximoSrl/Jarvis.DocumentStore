#if NETSTANDARD2_0_OR_GREATER
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client
{
    public class DocumentFormatReader
    {
        private readonly Uri _address;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenOptions _options;

        public Int64 ContentLength { get; private set; }
        public HttpResponseMessage ResponseData { get; private set; }

        public DocumentFormatReader(Uri address, IHttpClientFactory httpClientFactory, OpenOptions options = null)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _address = address;
        }

        public async Task<Stream> OpenStream()
        {
            var client = _httpClientFactory.CreateClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _address);

            if (_options != null)
            {
                if (_options.SkipContent)
                {
                    request.Method = HttpMethod.Head;
                }

                if (_options.RangeFrom.HasValue)
                {
                    request.Headers.Range = new RangeHeaderValue(_options.RangeFrom.Value, _options.RangeTo);
                }
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            this.ContentLength = response.Content.Headers.ContentLength ?? 0;
            this.ResponseData = response;

            return await response.Content.ReadAsStreamAsync();
        }
    }
}
#endif
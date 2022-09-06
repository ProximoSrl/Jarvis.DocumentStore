#if NET461
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client
{
    public class DocumentFormatReader
    {
        public Int64 ContentLength { get; private set; }

        public WebHeaderCollection ResponseData { get; private set; }

        private readonly HttpWebRequest _request;

        public DocumentFormatReader(Uri address, OpenOptions options = null)
        {
            _request = (HttpWebRequest)WebRequest.Create(address);
            if (options != null)
            {
                if (options.SkipContent)
                {
                    _request.Method = WebRequestMethods.Http.Head;
                }

                if (options.RangeFrom.HasValue)
                {
                    if (options.RangeTo.HasValue)
                        _request.AddRange(options.RangeFrom.Value, options.RangeTo.Value);
                    else
                        _request.AddRange(options.RangeFrom.Value);
                }
            }
        }

        public async Task<Stream> OpenStream()
        {
            var response = await _request.GetResponseAsync().ConfigureAwait(false);
            this.ContentLength = response.ContentLength;
            this.ResponseData = response.Headers;
            return response.GetResponseStream();
        }
    }
}
#endif
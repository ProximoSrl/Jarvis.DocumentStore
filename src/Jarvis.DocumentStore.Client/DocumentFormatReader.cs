using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client
{
    public class DocumentFormatReader : IDisposable
    {
        private readonly HttpWebRequest _request;

        public DocumentFormatReader(Uri address, OpenOptions options = null)
        {
            _request = (HttpWebRequest)WebRequest.Create(address);
            if (options != null)
            {
                if (options.RangeFrom > 0 || options.RangeTo > 0)
                {
                    _request.AddRange(options.RangeFrom, options.RangeTo);
                }
            }
        }

        public Task<Stream> ReadStream
        {
            get
            {
                var response = _request.GetResponse();
                return Task.FromResult(response.GetResponseStream());
            }
        }

        public void Dispose()
        {
        }
    }
}
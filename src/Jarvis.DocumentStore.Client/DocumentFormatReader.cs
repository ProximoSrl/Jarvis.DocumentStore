using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client
{
    public class DocumentFormatReader : IDisposable
    {
        public Int64 ContentLength { get; private set; }

        
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
                this.ContentLength = response.ContentLength;
                return Task.FromResult(response.GetResponseStream());
            }
        }

        public void Dispose()
        {
        }
    }
}
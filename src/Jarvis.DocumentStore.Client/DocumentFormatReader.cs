using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client
{
    public class DocumentFormatReader : IDisposable
    {
        readonly Uri _address;
        readonly WebClient _client;

        public DocumentFormatReader(Uri address)
        {
            _address = address;
            _client = new WebClient();
        }

        public Task<Stream> ReadStream
        {
            get
            {
                return _client.OpenReadTaskAsync(_address);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
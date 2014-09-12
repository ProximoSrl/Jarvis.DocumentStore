using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace Jarvis.ImageService.Host.Support
{
    public class ImageServiceApplication
    {
        IDisposable _webApplication;
        readonly Uri _serverAddress;

        public ImageServiceApplication(Uri serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public void Start()
        {
            _webApplication = WebApp.Start<ImageServicePipeline>(_serverAddress.AbsoluteUri);
        }

        public void Stop()
        {
            _webApplication.Dispose();
        }
    }
}

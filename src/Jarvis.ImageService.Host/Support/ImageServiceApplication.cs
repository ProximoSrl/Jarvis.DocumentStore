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
        protected IDisposable WebApplication;
        public void Start()
        {
            WebApplication = WebApp.Start<ImageServicePipeline>("http://localhost:5123");
        }

        public void Stop()
        {
            WebApplication.Dispose();
        }
    }
}

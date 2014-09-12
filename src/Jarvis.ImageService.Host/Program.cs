using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Host.Support;
using Topshelf;

namespace Jarvis.ImageService.Host
{
    class Program
    {
        static int Main(string[] args)
        {
            var exitCode = HostFactory.Run(host =>
            {
                host.Service<ImageServiceApplication>(service =>
                {
                    service.ConstructUsing(() => new ImageServiceApplication());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                host.SetDescription("Image service for JARVIS");
                host.SetDisplayName("Jarvis - Image service");
                host.SetServiceName("JarvisImageService");
                host.RunAsNetworkService();
            });

            return (int)exitCode;
        }
    }
}

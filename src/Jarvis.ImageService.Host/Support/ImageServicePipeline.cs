using System.Web.Http;
using Owin;

namespace Jarvis.ImageService.Host.Support
{
    public class ImageServicePipeline
    {
        public void Configuration(IAppBuilder application)
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            application.UseWebApi(config);
        }
    }
}
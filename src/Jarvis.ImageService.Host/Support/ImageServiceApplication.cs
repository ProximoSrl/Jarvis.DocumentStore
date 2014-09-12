using System.Web.Http;
using Castle.Windsor;
using Owin;

namespace Jarvis.ImageService.Host.Support
{
    public static class ContainerAccessor
    {
        public static IWindsorContainer Instance { get; set; }
    }

    public class ImageServiceApplication
    {
        public void Configuration(IAppBuilder application)
        {
            var config = new HttpConfiguration
            {
                DependencyResolver = new WindsorResolver(
                    ContainerAccessor.Instance
                    )
            };

            config.MapHttpAttributeRoutes();
            application.UseWebApi(config);
        }
    }
}
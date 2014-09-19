using System.Web.Http;
using Castle.Windsor;
using Jarvis.ImageService.Core.Http;
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
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new FileIdJsonConverter());

            application.UseWebApi(config);
        }
    }
}
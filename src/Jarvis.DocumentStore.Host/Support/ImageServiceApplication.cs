using System.Web.Http;
using Jarvis.DocumentStore.Core.Http;
using Owin;

namespace Jarvis.DocumentStore.Host.Support
{
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
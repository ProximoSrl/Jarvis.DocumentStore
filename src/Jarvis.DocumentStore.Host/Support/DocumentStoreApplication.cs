using System.Web.Http;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using Owin;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreApplication
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
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringValueJsonConverter());

            application.UseWebApi(config);
        }
    }
}
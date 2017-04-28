using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Logging;
using Jarvis.DocumentStore.Host.Support.Filters;
using Jarvis.Framework.Shared.Domain.Serialization;
using Metrics;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json.Serialization;
using Owin;
using Owin.Metrics;
using Swashbuckle.Application;
using System;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreApplication
    {
        private static DocumentStoreConfiguration _config;

        public static void SetConfig(DocumentStoreConfiguration config)
        {
            _config = config;
        }

        public void Configuration(IAppBuilder application)
        {
            if (_config == null)
                throw new ApplicationException("Configuration is null, you forget to call DocumentStoreApplication.SetConfig static initialization method");

            if (_config.IsApiServer)
            {
                ConfigureApi(application);
                ConfigureAdmin(application);
            }

            if (_config.HasMetersEnabled)
            {
                Metric
                    .Config
                    .WithOwin(middleware => application.Use(middleware),
                               config => config
                        .WithRequestMetricsConfig(c => c.WithAllOwinMetrics())
                        .WithMetricsEndpoint(endpointConfig => endpointConfig
                            .MetricsEndpoint("metrics/metrics")
                            .MetricsTextEndpoint("metrics/text")
                            .MetricsHealthEndpoint("metrics/health")
                            .MetricsJsonEndpoint("metrics/json")
                            .MetricsPingEndpoint("metrics/ping")
                        ));
            }
        }

        private void ConfigureAdmin(IAppBuilder application)
        {
            var appFolder = FindAppRoot();

            var fileSystem = new PhysicalFileSystem(appFolder);

            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = fileSystem,
                EnableDefaultFiles = true
            };

            application.UseFileServer(options);
        }

        private static string FindAppRoot()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory
                .ToLowerInvariant()
                .Split(System.IO.Path.DirectorySeparatorChar)
                .ToList();

            while (true)
            {
                var last = root.Last();
                if (last == String.Empty || last == "debug" || last == "release" || last == "bin" || last == "net461")
                {
                    root.RemoveAt(root.Count - 1);
                    continue;
                }

                break;
            }

            root.Add("app");

            var appFolder = String.Join("" + System.IO.Path.DirectorySeparatorChar, root);
            return appFolder;
        }

        private static void ConfigureApi(IAppBuilder application)
        {
            var config = new HttpConfiguration
            {
                DependencyResolver = new WindsorResolver(
                    ContainerAccessor.Instance
                )
            };

            config.MapHttpAttributeRoutes();
            var loggerFactory = ContainerAccessor.Instance.Resolve<IExtendedLoggerFactory>();

            var allowedIpList = _config.ExtraAllowedIpArray ?? Array.Empty<String>();

            var getOnlyAllowedIpList = _config.GetOnlyIpArray ?? Array.Empty<String>();

            config.Filters.Add(new LogFilterAttribute(loggerFactory.Create("LogFilter")));
            config.Filters.Add(new SecurityFilterAttribute(allowedIpList, getOnlyAllowedIpList));

            var jsonFormatter = new JsonMediaTypeFormatter();
            jsonFormatter.SerializerSettings.Converters.Add(new StringValueJsonConverter());
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Services.Replace(typeof(IContentNegotiator), new JsonContentNegotiator(jsonFormatter));

            config.Services.Add(
                typeof(IExceptionLogger),
                new Log4NetExceptionLogger(ContainerAccessor.Instance.Resolve<ILoggerFactory>())
            );

            var factory = ContainerAccessor.Instance.Resolve<IExtendedLoggerFactory>();

            config.MessageHandlers.Add(new TenantContextHandler(factory.Create(typeof(TenantContextHandler))));

            /* swagger */
            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Documenstore api"))
                .EnableSwaggerUi();

            application.UseWebApi(config);
        }
    }
}
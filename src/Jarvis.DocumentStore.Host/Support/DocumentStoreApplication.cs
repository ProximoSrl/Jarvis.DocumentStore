using System;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Host.Logging;
using Jarvis.Framework.Shared.Domain.Serialization;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreApplication
    {
        public void Configuration(IAppBuilder application)
        {
            ConfigureApi(application);
            ConfigureAdmin(application);
        }

        void ConfigureAdmin(IAppBuilder application)
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

        static string FindAppRoot()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory
                .ToLowerInvariant()
                .Split(Path.DirectorySeparatorChar)
                .ToList();

            while (true)
            {
                var last = root.Last();
                if (last == String.Empty || last == "debug" || last == "release" || last == "bin")
                {
                    root.RemoveAt(root.Count - 1);
                    continue;
                }

                break;
            }

            root.Add("app");

            var appFolder = String.Join("" + Path.DirectorySeparatorChar, root);
            return appFolder;
        }

        static void ConfigureApi(IAppBuilder application)
        {
            var config = new HttpConfiguration
            {
                DependencyResolver = new WindsorResolver(
                    ContainerAccessor.Instance
                )
            };

            config.MapHttpAttributeRoutes();

            var jsonFormatter = new JsonMediaTypeFormatter();
            jsonFormatter.SerializerSettings.Converters.Add(new StringValueJsonConverter());
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Services.Replace(typeof (IContentNegotiator), new JsonContentNegotiator(jsonFormatter));

            config.Services.Add(
                typeof(IExceptionLogger), 
                new Log4NetExceptionLogger(ContainerAccessor.Instance.Resolve<ILoggerFactory>())
            );

            var factory = ContainerAccessor.Instance.Resolve<IExtendedLoggerFactory>();

            config.MessageHandlers.Add(new TenantContextHandler(factory.Create(typeof(TenantContextHandler))));

            application.UseWebApi(config);
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Web.Http;
using CQRS.Shared.Domain;
using CQRS.Shared.Domain.Serialization;
using log4net.Repository.Hierarchy;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
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

            var appFolder = String.Join(""+Path.DirectorySeparatorChar, root);

            var fileSystem = new PhysicalFileSystem(appFolder);

            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = fileSystem,
                EnableDefaultFiles = true
            };

            application.UseFileServer(options);
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
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringValueJsonConverter());

            application.UseWebApi(config);
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficeToPdf
{
    public class OfficeToPdfConverterOptions
    {
        public OfficeToPdfConverterOptions()
        {
            SearchForParentFile(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
        }

        private void SearchForParentFile(DirectoryInfo currentDir)
        {
            while (currentDir != null)
            {
                var configFile = Path.Combine(currentDir.FullName, "MsGraphOfficeToPdf.config");
                if (File.Exists(configFile))
                {
                    var parsed = (JObject) JsonConvert.DeserializeObject(File.ReadAllText(configFile));   
                    TenantId = parsed["TenantId"].Value<string>();
                    ClientId = parsed["ClientId"].Value<string>();
                    ClientSecret = parsed["ClientSecret"].Value<string>();
                    SiteId = parsed["SiteId"].Value<string>();

                    Endpoint = parsed["Endpoint"].Value<string>();
                    GrantType = parsed["GrantType"].Value<string>();
                    Scope = parsed["Scope"].Value<string>();
                    Resource = parsed["Resource"].Value<string>();
                    GraphEndpoint = parsed["GraphEndpoint"].Value<string>();

                    return;
                }
                currentDir = currentDir.Parent;
            }
            throw new ConfigurationException("Unable to find configuration file MsGraphOfficeToPdf.config into a parent directory");
        }

        public string Endpoint { get; set; } 
        public string GrantType { get; set; }
        public string Scope { get; set; }
        public string Resource { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string GraphEndpoint { get; set; }

        public string SiteId { get; set; }
    }
}

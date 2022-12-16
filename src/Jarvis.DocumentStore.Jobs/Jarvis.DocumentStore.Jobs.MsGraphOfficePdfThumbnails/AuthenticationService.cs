using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails
{
    public class AuthenticationService
    {
        private readonly OfficeToPdfConverterOptions _options;

        public AuthenticationService(OfficeToPdfConverterOptions options)
        {
            _options = options;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("scope", _options.Scope),
                new KeyValuePair<string, string>("grant_type", _options.GrantType),
                new KeyValuePair<string, string>("resource", _options.Resource)
            };
            var client = new HttpClient();
            var requestUrl = $"{_options.Endpoint}{_options.TenantId}/oauth2/token";
            var requestContent = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(requestUrl, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenResponse = (JObject)JsonConvert.DeserializeObject(responseBody);
            return tokenResponse["access_token"].Value<string>();
        }
    }
}

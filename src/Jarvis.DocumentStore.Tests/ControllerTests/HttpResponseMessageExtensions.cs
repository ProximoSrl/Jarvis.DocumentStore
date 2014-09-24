using System;
using System.Net.Http;
using System.Web.Http;

namespace Jarvis.DocumentStore.Tests.ControllerTests
{
    public static class HttpResponseMessageExtensions
    {
        public static T UnWrap<T>(this HttpResponseMessage message)
        {
            var content = (ObjectContent<T>)message.EnsureSuccessStatusCode().Content;
            return (T)content.Value;
        }

        public static HttpError GetError(this HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
                throw new Exception(string.Format("Expected failure status code, found: {0}", (int)message.StatusCode));
            var content = (HttpError)((ObjectContent<HttpError>)message.Content).Value;
            return content;
        }
    }
}

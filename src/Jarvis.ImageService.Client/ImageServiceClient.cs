using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ImageService.Client
{
    public class ImageServiceClient
    {
        readonly Uri _apiRoot;

        public ImageServiceClient(Uri apiRoot)
        {
            _apiRoot = apiRoot;
        }

        public async Task Upload(string pathToFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(pathToFile);
            string fileNameWithExtension = Path.GetFileName(pathToFile);
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    using (var sourceStream = File.OpenRead(pathToFile))
                    {
                        content.Add(
                            new StreamContent(sourceStream),
                            fileName, fileNameWithExtension
                        );

//                        content.Add(new StringContent(pipeline), "pipeline");

                        var endPoint = new Uri(_apiRoot, "thumbnail/upload");

                        using (var message = await client.PostAsync(endPoint, content))
                        {
                            var input = message.Content.ReadAsStringAsync().Result;

                            message.EnsureSuccessStatusCode();
                        }
                    }
                }
            }
        }
    }
}

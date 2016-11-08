using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Model;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class FeedController : ApiController, ITenantController
    {
        readonly IReader<StreamReadModel, Int64> _streamReadModel;

        public FeedController(IReader<StreamReadModel, long> streamReadModel)
        {
            _streamReadModel = streamReadModel;
        }

        /// <summary>
        /// possible GET call
        /// http://localhost:5123/docs/feed/21/20?types=2&types=3
        /// 
        /// types is completely optional you can simply call
        /// 
        /// http://localhost:5123/docs/feed/21/20
        /// 
        /// if yoy are interested in all types of events.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="startId"></param>
        /// <param name="numOfResults"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{tenantId}/feed/{startId}/{numOfResults}")]
        public HttpResponseMessage GetFeed(TenantId tenantId, Int64 startId, Int32 numOfResults, [ModelBinder] List<Int32> types)
        {
            var baseQuery = _streamReadModel.AllUnsorted
                .Where(s => s.Id >= startId);

            if (types != null && types.Count > 0)
            {
                baseQuery = baseQuery.Where(r => types.Contains((Int32) r.EventType));
            }

            var result = baseQuery.Take(numOfResults)
                .ToList()
                .Select(rm => new FeedForStreamReadModel(rm))
                .ToList();
            var sc = new StringContent(JsonConvert.SerializeObject(result));
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var resp = new HttpResponseMessage { Content = sc };
            return resp;
        }
    }


}

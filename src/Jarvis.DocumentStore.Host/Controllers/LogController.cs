using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.DocumentStore.Host.Logging;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class LogSearchRequest
    {
        public string Level { get; set; }

        public int Page { get; set; }
        public int LogsPerPage { get; set; }

        public LogSearchRequest()
        {
            Page = 1;
            LogsPerPage = 10;
        }

        public bool IsEmpty
        {
            get
            {
                return String.IsNullOrWhiteSpace(Level);
            }
        }
    }

    public class LogSearchResponse
    {
        public IEnumerable<IDictionary<string, object>> Items { get; set; }
        public long Count { get; set; }
    }

    public class LogController : ApiController
    {
        private static MongoCollection Logs { get; set; }

        static LogController()
        {
            var appender = (IMongoAppenderCollectionProvider)
                LogManager.GetRepository()
                    .GetAppenders()
                    .First(x => x is IMongoAppenderCollectionProvider);

            Logs = appender.GetCollection();
        }


        [HttpPost]
        [Route("diagnostic/log")]
        public LogSearchResponse Get(LogSearchRequest request)
        {
            request = request ?? new LogSearchRequest();

            MongoCursor<BsonDocument> cursor;

            if (!request.IsEmpty)
            {
                var levels = request.Level.Split(',').Select(x=>x.Trim()).ToArray();
                cursor = Logs.FindAs<BsonDocument>(Query.In(FieldNames.Level, levels.Select(BsonValue.Create)));
            }
            else
            {
                cursor = Logs.FindAllAs<BsonDocument>();
            }

            var response = new LogSearchResponse
            {
                Items = cursor
                    .SetSortOrder(SortBy.Descending(FieldNames.Timestamp))
                    .SetSkip(request.LogsPerPage*(request.Page - 1))
                    .SetLimit(request.LogsPerPage)
                    .Select(x => x.ToDictionary()),
                Count = cursor.Count()
            };

            return response;
        }
    }
}

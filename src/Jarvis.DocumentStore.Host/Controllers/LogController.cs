using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.DocumentStore.Host.Logging;
using Jarvis.Framework.MongoAppender;
using log4net;
using log4net.Repository.Hierarchy;
using MongoDB.Bson;
using MongoDB.Driver;

using MongoDB.Driver.Linq;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class LogSearchRequest
    {
        public string Level { get; set; }
        public string Query { get; set; }
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
                return String.IsNullOrWhiteSpace(Level) && String.IsNullOrWhiteSpace(Query);
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
        private static IMongoCollection<BsonDocument> Logs { get; set; }

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
            IFindFluent<BsonDocument, BsonDocument> cursor;
            if (!request.IsEmpty)
            {
                List<FilterDefinition<BsonDocument>> queries = new List<FilterDefinition<BsonDocument>>();
                
                if (!String.IsNullOrWhiteSpace(request.Query))
                {
                    var queryExpr = new BsonRegularExpression(new Regex(request.Query, RegexOptions.IgnoreCase));
                    queries.Add(Builders< BsonDocument>.Filter.Or(
                        Builders< BsonDocument>.Filter.Regex(FieldNames.Message, queryExpr),
                        Builders<BsonDocument>.Filter.Regex(FieldNames.Loggername, queryExpr)
                    ));
                }

                if (!String.IsNullOrWhiteSpace(request.Level))
                {
                    var levels = request.Level.Split(',').Select(x => x.Trim()).ToArray();
                    queries.Add(Builders<BsonDocument>.Filter.In(FieldNames.Level, levels.Select(BsonValue.Create)));
                }

               cursor = Logs.Find(Builders<BsonDocument>.Filter.And(queries));

            }
            else
            {
                cursor = Logs.Find(Builders<BsonDocument>.Filter.Empty);
            }

            var response = new LogSearchResponse
            {
                Items = cursor
                    .Sort(Builders<BsonDocument>.Sort.Descending(FieldNames.Timestamp))
                    .Skip(request.LogsPerPage*(request.Page - 1))
                    .Limit(request.LogsPerPage)
                    .ToList()
                    .Select(x => x.ToDictionary()),
                Count = cursor.Count()
            };

            return response;
        }
    }
}

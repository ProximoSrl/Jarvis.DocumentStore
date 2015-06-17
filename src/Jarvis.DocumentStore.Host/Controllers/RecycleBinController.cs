using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.Framework.Shared.MultitenantSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Host.Controllers
{
    public class RecycleBinController : ApiController, ITenantController
    {
        public IRecycleBin RecycleBin { get; set; }

        public RecycleBinController()
        {

        }

        [Route("{tenantId}/recyclebin/documents")]
        [HttpPost]
        public RecycleBinResponse GetRecycleBin(
                TenantId tenantId,
                RecycleBinRequest request
            )
        {
            var page = request.Page - 1;
            var start = page * request.PageSize;
            var recycledDocuments = RecycleBin.Slots
                .Where(s => s.Id.StreamId.StartsWith("Document_"))
                .OrderByDescending(s => s.DeletedAt)
                .Skip(start)
                .Take(request.PageSize)
                .ToList()
                .Select(r =>
                {
                    return new RecycleBinData()
                    {
                        Handle = r.Data["Handle"].ToString(),
                        FileName = r.Data["FileName"] as String,
                        DeletedAt = r.DeletedAt,
                        CustomData = r.Data["CustomData"],
                    };
                })
                .ToList();

            var count = RecycleBin.Slots
                .Where(s => s.Id.StreamId.StartsWith("Document_"))
                .Count();

            return new RecycleBinResponse
            {
                Documents = recycledDocuments,
                Count = count,
            };
        }
    }

    public class RecycleBinResponse
    {
        public Int32 Count { get; set; }

        public List<RecycleBinData> Documents { get; set; }
    }
    public class RecycleBinData
    {
        public String Handle { get; set; }

        public String FileName { get; set; }

        public DateTime DeletedAt { get; set; }

        public Object CustomData { get; set; }
    }

    public class RecycleBinRequest
    {

        public int Page { get; set; }
        public int PageSize { get; set; }

        public String Filter { get; set; }

        public RecycleBinRequest()
        {
            Page = 1;
            PageSize = 30;
        }
    }
}

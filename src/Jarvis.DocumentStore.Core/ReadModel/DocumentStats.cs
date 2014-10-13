using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentStats : AbstractReadModel<string>
    {
        public long Bytes { get; set; }
        public int Files { get; set; }
    }
}

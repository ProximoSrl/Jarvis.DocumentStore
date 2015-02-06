using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Jobs
{

    public class QueuedJobDto
    {
       
        public String Id { get; set; }

        public Dictionary<String, String> Parameters { get; set; }

        public Dictionary<String, Object> HandleCustomData { get; set; }

    }
}

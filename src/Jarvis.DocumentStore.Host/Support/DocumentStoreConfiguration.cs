using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreConfiguration
    {
        public DocumentStoreConfiguration(bool isApiServer, bool isWorker, bool isReadmodelBuilder)
        {
            IsReadmodelBuilder = isReadmodelBuilder;
            IsWorker = isWorker;
            IsApiServer = isApiServer;
        }

        public bool IsApiServer { get; private set; }
        public bool IsWorker { get; private set; }
        public bool IsReadmodelBuilder { get; private set; }
    }
}

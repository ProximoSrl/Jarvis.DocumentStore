using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Host.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.Misc
{
    [TestFixture, Explicit]
    public class RemoteConfigurationServiceTests
    {
        [Test]
        public void configure_from_configuration_service()
        {
            var config = new RemoteDocumentStoreConfiguration();
        }
    }
}

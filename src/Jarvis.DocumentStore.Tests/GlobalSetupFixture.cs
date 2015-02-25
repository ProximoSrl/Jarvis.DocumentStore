using System.Reflection;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tests
{
    [SetUpFixture]
    public class GlobalSetupFixture
    {

        [SetUp]
        public void This_is_run_before_ANY_tests()
        {
            ////needed because we are trying to limit dependencies from mongo, and not want to use Serialization Attributes.
            //var actualSerialzier = BsonSerializer.LookupSerializer(typeof(TenantId));
            //if (!(actualSerialzier is StringValueBsonSerializer))
            //{
            //    BsonSerializer.RegisterSerializer(
            //        typeof(TenantId),
            //        new StringValueBsonSerializer()
            //   );
            //}
            try
            {
                var mngr = new IdentityManager(new CounterService(MongoDbTestConnectionProvider.ReadModelDb));
                mngr.RegisterIdentitiesFromAssembly(typeof (DocumentDescriptorId).Assembly);
                mngr.RegisterIdentitiesFromAssembly(typeof (TenantId).Assembly);
                mngr.RegisterIdentitiesFromAssembly(typeof (QueuedJobId).Assembly);

                EventStoreIdentityBsonSerializer.IdentityConverter = mngr;
                MongoFlatMapper.EnableFlatMapping();
            }
            catch (ReflectionTypeLoadException rle)
            {
                foreach (var ex in rle.LoaderExceptions)
                {
                    Console.WriteLine("Exception In typeloading: " + ex.Message);
                }
                Console.WriteLine("Exception in Global Setup: " + rle.ToString());
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in Global Setup: " + ex.ToString());
                throw;
            }

        }
    }
}

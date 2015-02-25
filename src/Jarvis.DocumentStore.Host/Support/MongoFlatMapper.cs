using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Domain;
using Jarvis.Framework.Shared.Domain.Serialization;
using Jarvis.Framework.Shared.Events;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Support
{
    public static class MongoFlatMapper
    {
        private static Boolean _enabled = false;

        public static void EnableFlatMapping() 
        {
            if (_enabled) return;

            BsonClassMap.RegisterClassMap<DomainEvent>(map =>
            {
                map.AutoMap();
                map.MapProperty(x => x.AggregateId).SetSerializer(new EventStoreIdentityBsonSerializer());
            });
            EventStoreIdentityCustomBsonTypeMapper.Register<DocumentDescriptorId>();
            EventStoreIdentityCustomBsonTypeMapper.Register<HandleId>();
            StringValueCustomBsonTypeMapper.Register<BlobId>();
            StringValueCustomBsonTypeMapper.Register<TenantId>();
            StringValueCustomBsonTypeMapper.Register<DocumentHandle>();
            StringValueCustomBsonTypeMapper.Register<FileHash>();
            StringValueCustomBsonTypeMapper.Register<QueuedJobId>();
            AddSerializerForAllStringBasedIdFromThisApplication();

            _enabled = true;
        }

        public static void AddSerializerForAllStringBasedIdFromAssembly(Assembly assembly) 
        {
            foreach (var type in assembly.GetTypes().Where(t => typeof(LowercaseStringValue).IsAssignableFrom(t)))
            {
                BsonSerializer.RegisterSerializer(type, new StringValueBsonSerializer());
            }
        }

        public static void AddSerializerForAllStringBasedIdFromThisApplication() 
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddSerializerForAllStringBasedIdFromAssembly(assembly);
            }
        }
    }

    
}

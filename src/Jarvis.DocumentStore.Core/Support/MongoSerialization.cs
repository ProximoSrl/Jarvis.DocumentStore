using Jarvis.DocumentStore.Client.Model;
using Jarvis.Framework.Shared.Domain.Serialization;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Support
{
    public class DocumentStoreStringValueSerializationProvider : IBsonSerializationProvider
    {
        public IBsonSerializer GetSerializer(Type type)
        {
            if (typeof(LowercaseClientAbstractStringValue).IsAssignableFrom(type) && !type.IsAbstract)
            {
                return new StringValueBsonSerializer(type);
            }

            return null;
        }
    }
}

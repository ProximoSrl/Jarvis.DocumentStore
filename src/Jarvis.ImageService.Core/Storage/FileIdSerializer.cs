using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Jarvis.ImageService.Core.Storage
{
    public class FileIdSerializer : IBsonSerializer 
    {
        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            var id= bsonReader.ReadString();
            return new FileId(id);
        }

        public IBsonSerializationOptions GetDefaultSerializationOptions()
        {
            throw new NotImplementedException();
        }

        public void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            var id = (FileId) value;
            bsonWriter.WriteString(id);
        }
    }
}

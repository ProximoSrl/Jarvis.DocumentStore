using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using Newtonsoft.Json;

namespace Jarvis.ImageService.Core.Http
{
    public class FileIdJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var id = (string)((FileId)value);
            writer.WriteValue(id);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var typedId = new FileId(Convert.ToString(reader.Value));
                return typedId;
            }
            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(FileId).IsAssignableFrom(objectType);
        }
    }
}
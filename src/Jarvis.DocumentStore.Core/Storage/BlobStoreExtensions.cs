using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.Messages;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Storage
{
    public static class BlobStoreExtensions
    {
        private static JsonSerializerSettings _settings;

        static BlobStoreExtensions()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                Converters = new JsonConverter[]
                {
                    new StringValueJsonConverter()
                },
                ContractResolver = new MessagesContractResolver(),
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };
        }

        public static BlobId Save<T>(this IBlobStore store, DocumentFormat format, T data)
        {
            BlobId id = null;
            using (var writer = store.CreateNew(format,new FileNameWithExtension(typeof(T).Name, "json")))
            {
                var stringValue = JsonConvert.SerializeObject(data,_settings);
                var bytes = Encoding.UTF8.GetBytes(stringValue);
                writer.WriteStream.Write(bytes,0, bytes.Length);
                id = writer.BlobId;
            }

            return id;
        }
    }
}

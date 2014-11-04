using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.Messages;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Shared.Serialization;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Storage
{
    public static class BlobStoreExtensions
    {
        static BlobStoreExtensions()
        {
        }

        public static BlobId Save<T>(this IBlobStore store, DocumentFormat format, T data)
        {
            using (var writer = store.CreateNew(format,new FileNameWithExtension(typeof(T).Name, "json")))
            {
                var stringValue = JsonConvert.SerializeObject(data, PocoSerializationSettings.Default);
                using (var sw = new StreamWriter(writer.WriteStream,Encoding.UTF8))
                {
                    sw.Write(stringValue);
                }
                return writer.BlobId;
            }
        }
    }
}

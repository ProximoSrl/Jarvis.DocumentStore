using System.Reflection;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jarvis.DocumentStore.Shared.Serialization
{
    public class PocoSerializationSettings : JsonSerializerSettings
    {
        private class PrivatePropertySetterResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                {
                    var property = member as PropertyInfo;
                    if (property != null)
                    {
                        var hasPrivateSetter = property.GetSetMethod(true) != null;
                        prop.Writable = hasPrivateSetter;
                    }
                }

                return prop;
            }
        }

        public static PocoSerializationSettings Default = new PocoSerializationSettings();
        public PocoSerializationSettings()
        {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full;
            ReferenceLoopHandling = ReferenceLoopHandling.Error;
            ContractResolver = new PrivatePropertySetterResolver();
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        }
    }
}
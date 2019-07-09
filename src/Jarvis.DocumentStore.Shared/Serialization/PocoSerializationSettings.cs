using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Jarvis.DocumentStore.Shared.Serialization
{
    public class PocoSerializationSettings : JsonSerializerSettings
    {
        public static PocoSerializationSettings Default { get; private set; } = new PocoSerializationSettings();

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

        public PocoSerializationSettings()
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full;
            ReferenceLoopHandling = ReferenceLoopHandling.Error;
            ContractResolver = new PrivatePropertySetterResolver();
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        }
    }
}
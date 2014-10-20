using System.Runtime.Serialization.Formatters;
using CQRS.Shared.Commands;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.Messages;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Processing
{
    public static class CommandSerializer
    {
        private static JsonSerializerSettings _settings;

        static CommandSerializer ()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                Converters = new JsonConverter[]
                {
                    new StringValueJsonConverter()
                },
                ContractResolver = new MessagesContractResolver(),
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };
        }

        public static string Serialize(ICommand command)
        {
            return JsonConvert.SerializeObject(command, _settings);
        }

        public static T Deserialize<T>(string command) where T : ICommand
        {
            return JsonConvert.DeserializeObject<T>(command, _settings);
        }
    }
}
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bencodex.Json;
using Bencodex.Types;

namespace Libplanet.Action.Sys
{
    internal sealed class SysActionJsonConverter : JsonConverter<IAction>
    {
        private static readonly BencodexJsonConverter BencodexJsonConverter
            = new BencodexJsonConverter();

        public override bool CanConvert(Type typeToConvert) =>
            typeof(IAction).IsAssignableFrom(typeToConvert);

        public override IAction? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            IValue? serialized = BencodexJsonConverter.Read(ref reader, typeToConvert, options);
            if (serialized is Bencodex.Types.Dictionary actionValues)
            {
                return Registry.Deserialize(actionValues);
            }

            throw new JsonException("Serialized action must be a Bencodex dictionary.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            IAction value,
            JsonSerializerOptions options
        )
        {
            Bencodex.Types.Dictionary serialized = Registry.Serialize(value);
            BencodexJsonConverter.Write(writer, serialized, options);
        }
    }
}

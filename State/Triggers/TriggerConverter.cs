using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Charr.Timers_BlishHUD.Models.Triggers
{
    // Enables JSON deserialization of the abstract Trigger class to the appropriate concrete child class
    // See: https://skrift.io/issues/bulletproof-interface-deserialization-in-jsonnet/
    public class TriggerConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Trigger);
        }
        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var trigger = default(Trigger);
            var type = jsonObject.Value<String>("type") ?? "location";
            switch (type)
            {
                case "key":
                    trigger = new KeyTrigger();
                    break;
                case "location":
                default:
                    trigger = new LocationTrigger();
                    break;
            }
            serializer.Populate(jsonObject.CreateReader(), trigger);
            return trigger;
        }
    }
}

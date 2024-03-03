using Charr.Timers_BlishHUD.Models.Triggers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Models.Actions
{
    public abstract class TimerAction
    {
        // Serialized Properties
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Action";
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("sets")] public List<string> TimerSets { get; set; } = new List<string>();

        [JsonConverter(typeof(TriggerConverter))]
        [JsonProperty("trigger")]
        public Trigger ActionTrigger { get; set; }


        // Non-serialized properties
        public bool Initialized
        {
            get { return _initialized; }
        }

        // Private members
        protected bool _initialized;

        public abstract string Initialize();
        public abstract void Update();
        public abstract void Dispose();
        public abstract void Start();
        public abstract void Stop();
        public abstract void Reset();
    }
}

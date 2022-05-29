using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Models.Triggers
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Trigger
    {
        // Serialized members
        [JsonProperty("type")]
        public String Type { get; }
        [JsonProperty("position")]
        public List<float> Position { get; set; }
        [JsonProperty("antipode")]
        public List<float> Antipode { get; set; }
        [JsonProperty("radius")]
        public float Radius { get; set; }
        [JsonProperty("requireCombat")]
        public bool CombatRequired { get; set; }
        [JsonProperty("requireOutOfCombat")]
        public bool OutOfCombatRequired { get; set; }
        [JsonProperty("requireEntry")]
        public bool EntryRequired { get; set; }
        [JsonProperty("requireDeparture")]
        public bool DepartureRequired { get; set; }

        // Non-serialized
        public bool Initialized
        {
            get { return _initialized; }
        }
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value)
                    Enable();
                else
                    Disable();
            }
        }

        // Protected members
        protected bool _initialized = false;
        protected bool _enabled = false;

        // Methods
        public abstract String Initialize();
        public abstract void Enable();
        public abstract void Disable();
        public abstract void Reset();
        public abstract bool Triggered();
    }
}

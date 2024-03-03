using Charr.Timers_BlishHUD.Pathing.Entities;
using Newtonsoft.Json;
using ProtoBuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charr.Timers_BlishHUD.Models.Timers
{
    public abstract class Timer
    {
        // Serialized Properties
        [JsonProperty("uid")] public string UID { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Timer";
        [JsonProperty("set")] public string TimerSet { get; set; } = "default";
        [JsonProperty("timestamps")] public List<float> Timestamps { get; set; }

        // Private
        protected bool _activated;
        protected bool _showTimer;

        // Non-serialized properties
        public bool Activated
        {
            get { return _activated; }
            set
            {
                if (value)
                    Activate();
                else
                    Deactivate();
            }
        }

        public void Update(Dictionary<string, float> elapsedTimes)
        {
            if (elapsedTimes.ContainsKey(TimerSet)) Update(elapsedTimes[TimerSet]);
        }
        public abstract void Update(float elapsedTime);
        public abstract void Activate();
        public abstract void Deactivate();
        public abstract void Stop();
    }
}

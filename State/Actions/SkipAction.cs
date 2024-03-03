

using Charr.Timers_BlishHUD.Pathing.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Models.Actions
{
    public class SkipAction : TimerAction
    {
        public SkipAction()
        {
            Name = "Skip Action";
            Type = "skipTime";
        }
        // Serialized Properties
        [JsonProperty("time")] public float Time { get; set; }

        // Non-serialized properties
        public float SkippedTime { get { return _skippedTime; } }

        // Private members
        private float _skippedTime = 0.0f;

        public override void Update()
        {
            if (ActionTrigger != null && ActionTrigger.Triggered())
            {
                _skippedTime+=Time;
                ActionTrigger.Reset();
            }
        }

        public override string Initialize()
        {
            if (TimerSets == null) return "no TimerSets.";
            if (ActionTrigger == null) return "no Triggers.";
            ActionTrigger.Initialize();
            _initialized = true;
            return null;
        }

        public override void Dispose()
        {
            ActionTrigger?.Reset();
            ActionTrigger?.Disable();
        }

        public override void Start()
        {
            ActionTrigger?.Reset();
            ActionTrigger?.Enable();
        }

        public override void Stop()
        {
            ActionTrigger?.Reset();
            ActionTrigger?.Disable();
        }

        public override void Reset()
        {
            ActionTrigger?.Reset();
            _skippedTime = 0.0f;
        }
    }
}

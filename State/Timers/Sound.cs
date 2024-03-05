using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Speech.Synthesis;

namespace Charr.Timers_BlishHUD.Models.Timers
{
    public class Sound : Timer
    {
        public Sound()
        {
            Name = "Unnamed Sound";
        }
        // Serialized Properties
        [JsonProperty("text")] public string Text { get; set; }

        // Private members
        private SpeechSynthesizer _synthesizer;
        private int timeIndex = 0;

        public string Initialize() {
            if (Text.IsNullOrEmpty())
                return Name + $" invalid text property";
            if (Timestamps.IsNullOrEmpty())
                return Name + " invalid timestamps property";

            Timestamps.Sort();

            _synthesizer = new SpeechSynthesizer();
            _synthesizer.Rate = -2;
            _synthesizer.Volume = 100;

            return null;
        }

        public override void Activate() {
            _activated = true;
        }

        public override void Deactivate() {
            _synthesizer.SpeakAsyncCancelAll();
            _activated = false;
        }

        public override void Stop() {
            _synthesizer.SpeakAsyncCancelAll();
        }

        public override void Update(float elapsedTime) {
            if (!_activated || TimersModule.ModuleInstance._hideSoundsSetting.Value) return;

            if (timeIndex < Timestamps.Count && elapsedTime >= Timestamps[timeIndex]) {
                _synthesizer.SpeakAsync(Text);
                timeIndex++;
            }
        }
    }
}

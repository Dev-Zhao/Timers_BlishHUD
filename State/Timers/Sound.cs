using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Speech.Synthesis;
using Blish_HUD;
using System;

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
            _synthesizer.Volume = TimersModule.ModuleInstance._volumeSetting?.Value ?? 100;
            TimersModule.ModuleInstance._volumeSetting.SettingChanged += HandleVolumeSettingChanged;

            return null;
        }

        private void HandleVolumeSettingChanged(object sender = null, EventArgs e = null)
        {
            _synthesizer.Volume = TimersModule.ModuleInstance._volumeSetting.Value;
        }

        public override void Activate() {
            _activated = true;
            timeIndex = 0;
        }

        public override void Deactivate() {
            _synthesizer.SpeakAsyncCancelAll();
            _activated = false;
            timeIndex = 0;
        }

        public override void Stop() {
            _synthesizer.SpeakAsyncCancelAll();
            timeIndex = 0;
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

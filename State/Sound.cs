using System.Collections.Generic;
using Blish_HUD;
using Charr.Timers_BlishHUD.Pathing.Content;
using Charr.Timers_BlishHUD.Pathing.Entities;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Speech.Synthesis;
using Microsoft.IdentityModel.Tokens;

namespace Charr.Timers_BlishHUD.State
{
    public class Sound
    {
        // Serialized Properties
        [JsonProperty("uid")] public string UID { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Marker";
        [JsonProperty("text")] public string Text { get; set; }
        [JsonProperty("timestamps")] public List<float> Timestamps { get; set; }


        // Non-serialized properties
        public bool Activated {
            get { return _activated; }
            set {
                if (value)
                    Activate();
                else
                    Deactivate();
            }
        }

        // Private members
        private bool _activated = false;
        private SpeechSynthesizer _synthesizer;

        public string Initialize() {
            if (Text.IsNullOrEmpty())
                return Name + " invalid text property";
            if (Timestamps.IsNullOrEmpty())
                return Name + " invalid timestamps property";

            _synthesizer = new SpeechSynthesizer();

            return null;
        }

        public void Activate() {
            _activated = true;
        }

        public void Deactivate() {
            _synthesizer.SpeakAsyncCancelAll();
            _activated = false;
        }

        public void Stop() {
            _synthesizer.SpeakAsyncCancelAll();
        }

        public void Update(float elapsedTime) {
            if (!_activated) return;

            _synthesizer.Volume = 100;
            _synthesizer.Rate = -2;

            foreach (float time in Timestamps) {
                if (elapsedTime >= time && elapsedTime <= time + TimersModule.ModuleInstance.Resources.TICKINTERVAL) {
                    _synthesizer.SpeakAsync(Text);
                    break;
                }
            }
        }
    }
}

using Blish_HUD;
using Charr.Timers_BlishHUD.Pathing.Content;
using Charr.Timers_BlishHUD.Pathing.Entities;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Models
{
    public class Marker
    {
        // Serialized Properties
        [JsonProperty("uid")] public string UID { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Marker";
        [JsonProperty("position")] public List<float> Position { get; set; }
        [JsonProperty("rotation")] public List<float> Rotation { get; set; }
        [JsonProperty("duration")] public float Duration { get; set; } = 10f;
        [JsonProperty("opacity")] public float Opacity { get; set; } = 0.8f;
        [JsonProperty("size")] public float Size { get; set; } = 1.0f;
        [JsonProperty("texture")] public string TextureString { get; set; }
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
        public bool ShowMarker {
            get { return _showMarker; }
            set {
                if (_markerPathable != null) {
                    _markerPathable.ShouldShow = value;
                }
                _showMarker = value;
            }
        }

        // Private members
        private MarkerPathable _markerPathable;
        private bool _activated;
        private bool _showMarker = true;

        public string Initialize(PathableResourceManager resourceManager) {
            if (Position == null || Position.Count != 3)
                return Name + " invalid position property";
            if (Timestamps == null || Timestamps.Count == 0)
                return Name + " invalid timestamps property";
            if (string.IsNullOrEmpty(TextureString))
                return Name + " invalid texture property";

            _markerPathable = new MarkerPathable {
                Opacity = Opacity,
                Rotation = (Rotation != null && Rotation.Count == 3)
                    ? new Vector3(Rotation[0], Rotation[1], Rotation[2])
                    : Vector3.Zero,
                Texture = resourceManager.LoadTexture(TextureString),
                Position = new Vector3(Position[0], Position[1], Position[2]),
                Size = new Vector2(Size, Size),
                BasicTitleText = Text,
                ShouldShow = ShowMarker
            };
            _markerPathable.Visible = false;

            return null;
        }

        public void Activate() {
            if (_markerPathable != null && !_activated) {
                GameService.Graphics.World.AddEntity(_markerPathable);
                _activated = true;
            }
        }

        public void Deactivate() {
            if (_markerPathable != null && _activated) {
                GameService.Graphics.World.RemoveEntity(_markerPathable);
                _activated = false;
            }
        }

        public void Stop() {
            if (_markerPathable != null) {
                _markerPathable.Visible = false;
            }
        }

        public void Update(float elapsedTime) {
            if (_markerPathable == null || !_activated) return;

            bool enabled = false;
            foreach (float time in Timestamps) {
                if (elapsedTime >= time && elapsedTime <= time + Duration) {
                    enabled = true;
                    _markerPathable.Visible = true;
                    break;
                }
            }

            if (!enabled) Stop();
        }
    }
}
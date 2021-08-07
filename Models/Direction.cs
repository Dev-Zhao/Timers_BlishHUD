using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Pathing.Entities;
using Charr.Timers_BlishHUD.Pathing.Content;
using Charr.Timers_BlishHUD.Pathing.Entities;
using Microsoft.Xna.Framework;

namespace Charr.Timers_BlishHUD.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class Direction {
        // Serialized properties
        [JsonProperty("uid")] public string UID { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Direction";
        [JsonProperty("position")] public List<float> Position { get; set; }

        [JsonProperty("destination")]
        public List<float> Destination {
            set { Position = value; }
        }

        [JsonProperty("duration")] public float Duration { get; set; } = 10f;
        [JsonProperty("opacity")] public float Opacity { get; set; } = 0.8f;
        [JsonProperty("animSpeed")] public float AnimSpeed { get; set; } = 1f;
        [JsonProperty("texture")] public string TextureString { get; set; }
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

        public bool ShowDirection {
            get { return _showDirection; }
            set {
                if (_trail != null) {
                    _trail.ShouldShow = value;
                }
                _showDirection = value;
            }
        }

        // Private members
        private TrailPathable _trail;
        private bool _activated;
        private bool _showDirection = true;

        public string Initialize(PathableResourceManager resourceManager) {
            if (Position == null || Position.Count != 3)
                return Name + " invalid position property";
            if (Timestamps == null || Timestamps.Count == 0)
                return Name + " invalid timestamps property";
            if (string.IsNullOrEmpty(TextureString))
                return Name + " invalid texture property";

            _trail = new TrailPathable {
                Opacity = Opacity,
                AnimationSpeed = AnimSpeed,
                TrailTexture = resourceManager.LoadTexture(TextureString),
                PointA = GameService.Gw2Mumble.PlayerCharacter.Position,
                PointB = new Vector3(Position[0], Position[1], Position[2]),
                ShouldShow = ShowDirection
            };
            _trail.Visible = false;

            return null;
        }

        public void Activate() {
            if (_trail != null && !_activated) {
                GameService.Graphics.World.AddEntity(_trail);
                _activated = true;
            }
        }

        public void Deactivate() {
            if (_trail != null && _activated) {
                GameService.Graphics.World.RemoveEntity(_trail);
                _activated = false;
            }
        }

        public void Stop() {
            if (_trail != null) {
                _trail.Visible = false;
            }
        }

        public void Update(float elapsedTime) {
            if (_trail == null || !_activated) return;

            bool enabled = false;
            foreach (float time in Timestamps) {
                if (elapsedTime >= time && elapsedTime <= time + Duration) {
                    enabled = true;
                    _trail.Visible = true;
                    _trail.PointA = GameService.Gw2Mumble.PlayerCharacter.Position;
                    break;
                }
            }

            if (!enabled) Stop();
        }
    }
}
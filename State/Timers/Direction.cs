using Blish_HUD;
using Charr.Timers_BlishHUD.Pathing.Content;
using Charr.Timers_BlishHUD.Pathing.Entities;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Charr.Timers_BlishHUD.Models.Timers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Direction : Timer
    {
        public Direction()
        {
            Name = "Unnamed Direction";
            _showTimer = true;
        }
        // Serialized properties
        [JsonProperty("position")] public List<float> Position { get; set; }

        [JsonProperty("destination")]
        public List<float> Destination {
            set { Position = value; }
        }

        [JsonProperty("duration")] public float Duration { get; set; } = 10f;
        [JsonProperty("opacity")] public float Opacity { get; set; } = 0.8f;
        [JsonProperty("animSpeed")] public float AnimSpeed { get; set; } = 1f;
        [JsonProperty("texture")] public string TextureString { get; set; }

        // Non-serialized properties
        public bool ShowDirection {
            get { return _showTimer; }
            set {
                if (_trail != null) {
                    _trail.ShouldShow = value;
                }
                _showTimer = value;
            }
        }

        // Private members
        private TrailPathable _trail;

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

        public override void Activate() {
            if (_trail != null && !_activated) {
                GameService.Graphics.World.AddEntity(_trail);
                _activated = true;
            }
        }

        public override void Deactivate() {
            if (_trail != null && _activated) {
                GameService.Graphics.World.RemoveEntity(_trail);
                _activated = false;
            }
        }

        public override void Stop() {
            if (_trail != null) {
                _trail.Visible = false;
            }
        }

        public override void Update(float elapsedTime) {
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
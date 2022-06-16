using Charr.Timers_BlishHUD.Models.Triggers;
using Charr.Timers_BlishHUD.Pathing.Content;
using Charr.Timers_BlishHUD.State;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Blish_HUD;

namespace Charr.Timers_BlishHUD.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Phase : IDisposable
    {
        // Serialized Properties
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Phase";

        [JsonConverter(typeof(TriggerConverter))]
        [JsonProperty("start")]
        public Trigger StartTrigger { get; set; }

        [JsonConverter(typeof(TriggerConverter))]
        [JsonProperty("finish")]
        public Trigger FinishTrigger { get; set; }

        [JsonProperty("alerts")] public List<Alert> Alerts { get; set; }
        [JsonProperty("directions")] public List<Direction> Directions { get; set; } = new List<Direction>();
        [JsonProperty("markers")] public List<Marker> Markers { get; set; } = new List<Marker>();
        [JsonProperty("sounds")] public List<Sound> Sounds { get; set; } = new List<Sound>();

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
        public bool ShowAlerts {
            get { return _showAlerts; }
            set {
                Alerts?.ForEach(al => al.ShowAlert = value);
                _showAlerts = value;
            }
        }
        public bool ShowDirections {
            get { return _showDirections; }
            set {
                Directions?.ForEach(dir => dir.ShowDirection = value);
                _showDirections = value;
            }
        }
        public bool ShowMarkers {
            get { return _showMarkers; }
            set {
                Markers?.ForEach(mark => mark.ShowMarker = value);
                _showMarkers = value;
            }
        }

        public bool Active { get; private set; } = false;

        // Private members
        private bool _activated;
        private bool _showAlerts = true;
        private bool _showDirections = true;
        private bool _showMarkers = true;
        private static readonly Logger Logger = Logger.GetLogger<Phase>();

        public string Initialize(PathableResourceManager pathableResourceManager) {
            if (StartTrigger == null) return "phase missing start trigger";

            String message = StartTrigger.Initialize();
            if (message != null) return message;

            if (FinishTrigger != null) {
                message = FinishTrigger.Initialize();
                if (message != null) return message;
            }

            // Validation & Initialization
            if (Alerts != null) {
                foreach (Alert al in Alerts) {
                    message = al.Initialize(pathableResourceManager);
                    al.ShowAlert = ShowAlerts;
                    if (message != null) return message;
                }
            }

            if (Directions != null) {
                foreach (Direction dir in Directions) {
                    message = dir.Initialize(pathableResourceManager);
                    dir.ShowDirection = ShowDirections;
                    if (message != null) return message;
                }
            }

            if (Markers != null) {
                foreach (Marker mark in Markers) {
                    message = mark.Initialize(pathableResourceManager);
                    mark.ShowMarker = ShowMarkers;
                    if (message != null) return message;
                }
            }

            if (Sounds != null) {
                foreach (Sound voice in Sounds) {
                    message = voice.Initialize();
                    if (message != null) return message;
                }
            }

            return null;
        }

        public void Activate() {
            if (_activated) return;
            Alerts?.ForEach(al => {
                if (!string.IsNullOrEmpty(al.UID)) {
                    if (!TimersModule.ModuleInstance._activeAlertIds.ContainsKey(al.UID)) {
                        al.Activate();
                        TimersModule.ModuleInstance._activeAlertIds.Add(al.UID, al);
                    }
                }
                else {
                    al.Activate();
                }
            });
            Directions?.ForEach(dir => {
                if (!string.IsNullOrEmpty(dir.UID)) {
                    if (!TimersModule.ModuleInstance._activeDirectionIds.ContainsKey(dir.UID)) {
                        dir.Activate();
                        TimersModule.ModuleInstance._activeDirectionIds.Add(dir.UID, dir);
                    }
                }
                else {
                    dir.Activate();
                }
            });
            Markers?.ForEach(mark => {
                if (!string.IsNullOrEmpty(mark.UID)) {
                    if (!TimersModule.ModuleInstance._activeMarkerIds.ContainsKey(mark.UID)) {
                        mark.Activate();
                        TimersModule.ModuleInstance._activeMarkerIds.Add(mark.UID, mark);
                    }
                }
                else {
                    mark.Activate();
                }
            });

            Sounds.ForEach(voice => voice.Activate());
            _activated = true;
        }

        public void Deactivate() {
            if (!_activated) return;

            Stop();
            Alerts?.ForEach(al => {
                if (!string.IsNullOrEmpty(al.UID)) {
                    Alert activeAlert;
                    if (TimersModule.ModuleInstance._activeAlertIds.TryGetValue(al.UID, out activeAlert) && activeAlert == al) {
                        TimersModule.ModuleInstance._activeAlertIds.Remove(al.UID);
                    }
                }
                al.Deactivate();
            });
            Directions?.ForEach(dir => {
                if (!string.IsNullOrEmpty(dir.UID)) {
                    Direction activeDirection;
                    if (TimersModule.ModuleInstance._activeDirectionIds.TryGetValue(dir.UID, out activeDirection) && activeDirection == dir) {
                        TimersModule.ModuleInstance._activeDirectionIds.Remove(dir.UID);
                    }
                }
                dir.Deactivate();
            });
            Markers?.ForEach(mark => {
                if (!string.IsNullOrEmpty(mark.UID)) {
                    Marker activeMarker;
                    if (TimersModule.ModuleInstance._activeMarkerIds.TryGetValue(mark.UID, out activeMarker) && activeMarker == mark) {
                        TimersModule.ModuleInstance._activeMarkerIds.Remove(mark.UID);
                    }
                }
                mark.Deactivate();
            });
            Sounds?.ForEach(voice => voice.Deactivate());
            _activated = false;
        }

        public void WaitForStart() {
            StartTrigger?.Enable();
            Debug.WriteLine(Name + " phase waiting");
            Logger.Warn(Name + " phase waiting");
        }

        public void Start() {
            // Phase has started, enable finish trigger to check for finish conditions
            if (Active || !_activated) return;

            StartTrigger?.Reset();
            StartTrigger?.Disable();

            FinishTrigger?.Enable();

            Active = true;
            Debug.WriteLine(Name + " phase starting");
            Logger.Warn(Name + " phase starting");
        }

        public void Stop() {
            if (!Active) return;

            StartTrigger?.Reset();
            StartTrigger?.Disable();

            FinishTrigger?.Reset();
            FinishTrigger?.Disable();

            Alerts?.ForEach(al => {
                if (!string.IsNullOrEmpty(al.UID)) {
                    Alert activeAlert;
                    if (TimersModule.ModuleInstance._activeAlertIds.TryGetValue(al.UID, out activeAlert) && activeAlert == al) {
                        TimersModule.ModuleInstance._activeAlertIds.Remove(al.UID);
                    }
                }
                al.Stop();
            });

            Directions?.ForEach(dir => {
                if (!string.IsNullOrEmpty(dir.UID)) {
                    Direction activeDirection;
                    if (TimersModule.ModuleInstance._activeDirectionIds.TryGetValue(dir.UID, out activeDirection) && activeDirection == dir) {
                        TimersModule.ModuleInstance._activeDirectionIds.Remove(dir.UID);
                    }
                }
                dir.Stop();
            });

            Markers?.ForEach(mark => {
                if (!string.IsNullOrEmpty(mark.UID)) {
                    Marker activeMarker;
                    if (TimersModule.ModuleInstance._activeMarkerIds.TryGetValue(mark.UID, out activeMarker) && activeMarker == mark) {
                        TimersModule.ModuleInstance._activeMarkerIds.Remove(mark.UID);
                    }
                }
                mark.Stop();
            });

            Sounds?.ForEach(voice => voice.Stop());
            Active = false;

            Logger.Warn(Name + " phase stopping");
        }

        public void Update(float elapsedTime) {
            Alerts?.ForEach(al => al.Update(elapsedTime));
            Directions?.ForEach(dir => dir.Update(elapsedTime));
            Markers?.ForEach(mark => mark.Update(elapsedTime));
            Sounds?.ForEach(voice => voice.Update(elapsedTime));
        }

        public void Dispose() {
            Deactivate();
            Alerts?.ForEach(al => al?.Dispose());
            Directions?.Clear();
            Markers?.Clear();
            Alerts?.Clear();
            Sounds?.Clear();
        }
    }
}
using Blish_HUD;
using Blish_HUD.Content;
using Charr.Timers_BlishHUD.Models.Triggers;
using Charr.Timers_BlishHUD.Pathing.Content;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Charr.Timers_BlishHUD.Models {
    public class TimerReadException : Exception {
        public TimerReadException() {
        }

        public TimerReadException(string message) : base(message) {
        }

        public TimerReadException(string message, Exception inner) : base(message, inner) {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Encounter : IDisposable {
        // Serialized properties
        [JsonProperty("id")] public string Id { get; set; } = "Unknown Id";
        [JsonProperty("name")] public string Name { get; set; } = "Unknown Timer";
        [JsonProperty("category")] public string Category { get; set; } = "Other";
        [JsonProperty("description")] public string Description { get; set; } = "Timer description has not been set.";
        [JsonProperty("author")] public string Author { get; set; } = "Unknown Author";
        [JsonProperty("icon")] public string IconString { get; set; } = "raid";

        [JsonProperty("enabled")] public bool Enabled { get; set; } = false;
        [JsonProperty("map")] public int Map { get; set; }
        [JsonProperty("phases")] public List<Phase> Phases { get; set; }

        [JsonConverter(typeof(TriggerConverter))]
        [JsonProperty("reset")]
        public Trigger ResetTrigger { get; set; }

        // Non-serialized
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
                Phases?.ForEach(ph => ph.ShowAlerts = value);
                _showAlerts = value;
            }
        }
        public bool ShowDirections {
            get { return _showDirections; }
            set {
                Phases?.ForEach(ph => ph.ShowDirections = value);
                _showDirections = value;
            }
        }
        public bool ShowMarkers {
            get { return _showMarkers; }
            set {
                Phases?.ForEach(ph => ph.ShowMarkers = value);
                _showMarkers = value;
            }
        }

        public bool Active { get; private set; } = false;
        public bool Valid { get; private set; } = false;
        public AsyncTexture2D Icon { get; set; }

        public bool IsFromZip { get; set; } = false;
        private string _zipFile;
        public string ZipFile {
            get => _zipFile;
            set {
                _zipFile = value;
                if (_zipFile != String.Empty) {
                    IsFromZip = true;
                }
            }
        }
        public string TimerFile { get; set; }

        // Private members
        private bool _activated = false;
        private bool _awaitingNextPhase = false;
        private bool _showAlerts = true;
        private bool _showMarkers = true;
        private bool _showDirections = true;
        private int _currentPhase = 0;
        private DateTime _startTime;
        private DateTime _lastUpdate;
        private readonly int TICKRATE = 100;

        public override bool Equals(Object obj) {
            if ((obj == null) || !(this.GetType() == obj.GetType())) {
                return false;
            }
            else {
                Encounter enc = (Encounter)obj;
                return (this.Id == enc.Id);
            }
        }

        public void Initialize(PathableResourceManager resourceManager) {
            Icon = TimersModule.ModuleInstance.Resources.GetIcon(IconString);
            if (Icon == null)
                Icon = resourceManager.LoadTexture(IconString);

            if (Map <= 0)
                throw new TimerReadException("Map property undefined/invalid");
            if (Phases == null || Phases.Count == 0)
                throw new TimerReadException("Phase property undefined");
            if (ResetTrigger == null)
                throw new TimerReadException("Reset property is undefined");

            string message = ResetTrigger.Initialize();
            if (message != null)
                throw new TimerReadException("Reset trigger invalid - " + message);

            foreach (Phase ph in Phases) {
                message = ph.Initialize(resourceManager);
                ph.ShowAlerts = ShowAlerts;
                ph.ShowDirections = ShowDirections;
                ph.ShowMarkers = ShowMarkers;
                if (message != null)
                    throw new TimerReadException(Id + ": " + message);
            }

            Valid = true;
        }

        private void Activate() {
            if (!Enabled || _activated) return;

            ResetTrigger.Enable();

            Phases.ForEach(ph => ph.Activate());
            Phases[0].WaitForStart();
            _activated = true;
            Debug.WriteLine(Name + " activated!");
        }

        private void Deactivate() {
            if (!_activated) return;

            Stop();
            Phases.ForEach(ph => ph.Deactivate());
            _activated = false;
        }

        private bool ShouldStart() {
            if (Active || !Enabled || !Activated) return false;

            if (Map != GameService.Gw2Mumble.CurrentMap.Id)
                return false;

            Phase first = Phases[0];
            ResetTrigger.Enable();
            return first.StartTrigger.Triggered();
        }

        private bool ShouldStop() {
            if (!Active) return false;

            if (Map != GameService.Gw2Mumble.CurrentMap.Id) {
                Debug.WriteLine("bug3");
                return true;
            }


            if (_currentPhase == (Phases.Count - 1) &&
                Phases[_currentPhase].FinishTrigger != null &&
                Phases[_currentPhase].FinishTrigger.Triggered()) {
                Debug.WriteLine("bug2");
                return true;
            }


            Debug.WriteLine("stop: " + ResetTrigger.Triggered());
            return ResetTrigger.Triggered();
        }

        private void Start() {
            if (Active || !Enabled || !Activated) return;

            _startTime = DateTime.Now;
            Active = true;
            Phases[_currentPhase].Start();
            Phases[_currentPhase].Update(0.0f);
            _lastUpdate = DateTime.Now;
        }

        private void Stop() {
            Debug.WriteLine("active " + Active);
            if (!Active) return;

            Phases.ForEach(ph => ph.Stop());
            Active = false;
            _currentPhase = 0;
            _awaitingNextPhase = false;
            ResetTrigger.Disable();
            ResetTrigger.Reset();
        }

        public void Update(GameTime gameTime) {
            if (ShouldStart()) {
                Start();
                Debug.WriteLine("Start");
            }
            else if (ShouldStop()) {
                Debug.WriteLine("Stop");
                Stop();
                if (Enabled && Map == GameService.Gw2Mumble.CurrentMap.Id) {
                    Phases[0].WaitForStart();
                    ResetTrigger.Enable();
                }
            }
            else if (_awaitingNextPhase) {
                // Waiting period between phases.
                if (_currentPhase + 1 < Phases.Count) {
                    if (Phases[_currentPhase + 1].StartTrigger != null &&
                        Phases[_currentPhase + 1].StartTrigger.Triggered()) {
                        _currentPhase++;
                        _awaitingNextPhase = false;
                        Start();
                    }
                }
            }
            else if (Phases[_currentPhase].FinishTrigger != null &&
                     Phases[_currentPhase].FinishTrigger.Triggered()) {
                // Transition to waiting period between phases.
                _awaitingNextPhase = true;
                Phases[_currentPhase].Stop();
                Active = false;
                if (_currentPhase + 1 < Phases.Count) {
                    Phases[_currentPhase + 1].WaitForStart();
                }
            }
            else if (Active && (DateTime.Now - _lastUpdate).TotalSeconds >= TimersModule.ModuleInstance.Resources.TICKINTERVAL) {
                // Phase updates.
                float elapsedTime = (float) (DateTime.Now - _startTime).TotalSeconds;
                _lastUpdate = DateTime.Now;
                Phases[_currentPhase].Update(elapsedTime);
            }
        }

        public void Dispose() {
            Deactivate();
            Phases?.ForEach(ph => ph?.Dispose());
            Phases?.Clear();
        }
    }
}
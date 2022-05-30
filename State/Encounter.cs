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

        public AsyncTexture2D Icon { get; set; } = ContentService.Textures.TransparentPixel;

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

        public enum EncounterStates {
            Error,
            Suspended,
            Ready,
            WaitingToRun,
            Running,
            WaitingNextPhase
        }

        public EncounterStates State = EncounterStates.Error;
        private int _currentPhase = 0;
        private DateTime _startTime;
        private DateTime _lastUpdate;

        private bool _showAlerts = true;
        private bool _showMarkers = true;
        private bool _showDirections = true;

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

            State = EncounterStates.Ready;
        }

        public void Activate() {
            if (!Enabled || State > EncounterStates.Ready) return;

            State = EncounterStates.Ready;

            if (Map == GameService.Gw2Mumble.CurrentMap.Id) {
                Phases.ForEach(ph => ph.Activate());
                Phases[0].WaitForStart();

                State = EncounterStates.WaitingToRun;
            }

            Debug.WriteLine(Name + " activated!");
        }

        public void Deactivate() {
            if (State == EncounterStates.Suspended) return;

            Stop();
            Phases?.ForEach(ph => ph.Deactivate());
            State = EncounterStates.Suspended;
        }

        private bool ShouldRun() {
            if (!Enabled || State != EncounterStates.WaitingToRun) return false;

            if (Map != GameService.Gw2Mumble.CurrentMap.Id)
                return false;

            Phase first = Phases[0];
            return first.StartTrigger.Triggered();
        }

        private bool ShouldStop() {
            if (State != EncounterStates.Running && State != EncounterStates.WaitingNextPhase) return false;

            if (Map != GameService.Gw2Mumble.CurrentMap.Id) {
                return true;
            }

            if (_currentPhase == (Phases.Count - 1) &&
                Phases[_currentPhase].FinishTrigger != null &&
                Phases[_currentPhase].FinishTrigger.Triggered()) {
                return true;
            }

            return ResetTrigger.Triggered();
        }

        private void Run() {
            if (!Enabled || State != EncounterStates.WaitingToRun) return;

            ResetTrigger.Enable();

            _startTime = DateTime.Now;
            Phases[_currentPhase].Start();
            Phases[_currentPhase].Update(0.0f);
            _lastUpdate = DateTime.Now;

            State = EncounterStates.Running;
        }

        private void Stop() {
            if (State == EncounterStates.Suspended) return;

            Phases?.ForEach(ph => ph.Stop());
            _currentPhase = 0;
            ResetTrigger?.Disable();
            ResetTrigger?.Reset();
            State = EncounterStates.Suspended;
        }

        public void Update(GameTime gameTime) {
            if (ShouldRun()) {
                Run();
                Debug.WriteLine("Start");
            }
            else if (ShouldStop()) {
                Debug.WriteLine("Stop");
                Stop();
                if (Enabled && Map == GameService.Gw2Mumble.CurrentMap.Id) {
                    Phases[0].WaitForStart();
                    State = EncounterStates.WaitingToRun;
                }
            } 
            else if (State == EncounterStates.WaitingNextPhase) {
                // Waiting period between phases.
                if (_currentPhase + 1 < Phases.Count) {
                    if (Phases[_currentPhase + 1].StartTrigger != null &&
                        Phases[_currentPhase + 1].StartTrigger.Triggered()) {
                        _currentPhase++;
                        State = EncounterStates.Running;
                    }
                }
            }
            else if (Phases[_currentPhase].FinishTrigger != null &&
                     Phases[_currentPhase].FinishTrigger.Triggered()) {
                // Transition to waiting period between phases.
                Phases[_currentPhase].Stop();
                if (_currentPhase + 1 < Phases.Count) {
                    Phases[_currentPhase + 1].WaitForStart();
                    State = EncounterStates.WaitingNextPhase;
                }
            }

            if (State == EncounterStates.Running && (DateTime.Now - _lastUpdate).TotalSeconds >= TimersModule.ModuleInstance.Resources.TICKINTERVAL) {
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